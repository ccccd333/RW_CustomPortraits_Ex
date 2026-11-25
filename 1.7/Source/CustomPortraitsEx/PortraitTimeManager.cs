using CustomPortraits;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    /// <summary>
    /// FPS計測、動画のステップモード移行判定、およびアニメーション画像のフレームスキップ管理を行うユーティリティ。
    /// OnGUI は 1 描画フレームに複数回（Layout / Repaint / 入力イベント）呼ばれるため、
    /// Time.frameCount で 1 描画フレームあたり 1 回のみ計測・判定を行う。
    /// </summary>
    public static class PortraitTimeManager
    {
        // ---- FPS計測 & ステップモード用 ----
        private static int _last_fps_frame = -1;
        private static float _prev_frame_time = 0f;
        private static float _measured_fps = 60f;
        private static readonly Queue<float> _fps_history = new Queue<float>();
        private static bool _low_fps_detected = false;

        private static int _high_fps_count = 0;

        public static bool LowFpsDetected => _low_fps_detected;

        /// <summary>
        /// 動画ストール検知時などに外部からステップモード状態へ強制設定する。
        /// </summary>
        public static void ForceStepMode()
        {
            _low_fps_detected = true;
            _high_fps_count = 0;
        }

        // ---- アニメーションスキップ用 ----
        private static float _last_update_time = Time.realtimeSinceStartup;

        // ---- 動画ストール検知用 ----
        private static int _last_stall_frame = -1;
        private static long _last_video_frame = -1;
        private static float _last_video_frame_advance_time = 0f;
        private static int _stalled_render_frames = 0;

        public static void ResetVideoStallTimer()
        {
            _last_stall_frame = -1;
            _last_video_frame = -1;
            _last_video_frame_advance_time = Time.realtimeSinceStartup;
            _stalled_render_frames = 0;
        }

        public static void ResetAllTimers()
        {
            _last_update_time = Time.realtimeSinceStartup;
            _last_fps_frame = -1;
            _prev_frame_time = 0f;
            _low_fps_detected = false;
            _high_fps_count = 0;
            _fps_history.Clear();
            ResetVideoStallTimer();
        }

        /// <summary>
        /// 動画のフレーム停止（ストール）を検知する。
        /// 描画フレーム単位で設定回数以上かつ実時間で設定秒数以上フレームが進んでいない場合にストールと判定し、
        /// ステップモード状態へ強制設定した上で true を返す。
        /// </summary>
        public static bool CheckVideoStall(bool isVideoPlaying, bool hasFrame, bool isVideoEnded, long currentFrame)
        {
            if (!isVideoPlaying || !hasFrame || isVideoEnded || currentFrame < 0)
                return false;

            int frame_count = Time.frameCount;
            if (_last_stall_frame == frame_count)
                return false;
            _last_stall_frame = frame_count;

            float now = Time.realtimeSinceStartup;

            if (currentFrame != _last_video_frame)
            {
                // フレームが正常に進んでいる
                _last_video_frame = currentFrame;
                _last_video_frame_advance_time = now;
                _stalled_render_frames = 0;
                return false;
            }

            // フレームが前回描画フレームから動いていない
            _stalled_render_frames++;

            if (_stalled_render_frames >= PortraitCacheEx.Settings.video_stall_trigger_count &&
                (now - _last_video_frame_advance_time) >= PortraitCacheEx.Settings.video_stall_trigger_seconds)
            {
                if (Settings.Instance.debug)
                {
                    Log.Message($"[PortraitsEx] Video stall detected at frame {currentFrame} (stalled {_stalled_render_frames} frames, {now - _last_video_frame_advance_time:F2}s). Switching to step mode...");
                }

                ForceStepMode();

                _last_video_frame_advance_time = now;
                _stalled_render_frames = 0;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 1描画フレームに1回だけ実フレーム間隔からFPSを計測し、低FPS状態かどうかを判定する。
        /// 条件を満たした場合にステップモード／通常再生への移行コールバックを実行する。
        /// </summary>
        public static void UpdateFpsMeasurement(bool isVideoPlaying, bool isVideoStepMode, System.Action onTransitionToStepMode, System.Action onTransitionToPlayMode)
        {
            int frame_count = Time.frameCount;
            if (_last_fps_frame == frame_count)
                return;
            _last_fps_frame = frame_count;

            float now = Time.realtimeSinceStartup;
            if (_prev_frame_time > 0f)
            {
                float delta = now - _prev_frame_time;
                if (delta > 0.00001f)
                {
                    _measured_fps = 1f / delta;
                    _fps_history.Enqueue(_measured_fps);

                    while (_fps_history.Count > PortraitCacheEx.Settings.video_fps_history_size)
                        _fps_history.Dequeue();
                }
            }
            _prev_frame_time = now;

            if (_fps_history.Count == 0)
                return;

            float threshold = PortraitCacheEx.Settings.video_step_mode_fps_threshold;
            if (_measured_fps < threshold)
            {
                int low_fps_count = 0;
                foreach (float fps in _fps_history)
                {
                    if (fps < threshold)
                        low_fps_count++;
                }

                if (low_fps_count >= PortraitCacheEx.Settings.video_step_mode_trigger_count)
                {
                    _high_fps_count = 0;
                    _low_fps_detected = true;

                    if (isVideoPlaying && !isVideoStepMode)
                    {
                        if (Settings.Instance.debug)
                            Log.Message($"[CustomPortraitsEx] Transitioning to step mode due to low FPS. {_measured_fps:F2} fps. low_fps_count: {low_fps_count} trigger count: {PortraitCacheEx.Settings.video_step_mode_trigger_count}");
                        onTransitionToStepMode?.Invoke();
                    }
                }
            }
            else
            {
                if (_high_fps_count < PortraitCacheEx.Settings.video_play_mode_trigger_count)
                {
                    _high_fps_count++;
                }

                if (_high_fps_count >= PortraitCacheEx.Settings.video_play_mode_trigger_count)
                {
                    if (isVideoPlaying && isVideoStepMode)
                    {
                        if (Settings.Instance.debug)
                            Log.Message($"[CustomPortraitsEx] FPS recovered: {_measured_fps:F2} fps. Transitioning back to play mode. high_fps_count: {_high_fps_count} trigger count: {PortraitCacheEx.Settings.video_play_mode_trigger_count}");
                        onTransitionToPlayMode?.Invoke();
                    }

                    _low_fps_detected = false;
                }
            }
        }

        /// <summary>
        /// 画像アニメーションのフレーム経過判定と、ラグ時のスキップ枚数を計算する。
        /// </summary>
        public static bool CheckAnimationInterval(float frameIntervalSeconds, bool skipOnLag, out int skipCount)
        {
            skipCount = 1;
            float currentTime = Time.realtimeSinceStartup;

            if (currentTime - _last_update_time >= frameIntervalSeconds)
            {
                if (skipOnLag && frameIntervalSeconds > 0.0001f)
                {
                    float delta = currentTime - _last_update_time;
                    skipCount = Mathf.FloorToInt(delta / frameIntervalSeconds);
                    _last_update_time += skipCount * frameIntervalSeconds;
                }
                else
                {
                    _last_update_time = currentTime;
                }
                return true;
            }

            return false;
        }
    }
}
