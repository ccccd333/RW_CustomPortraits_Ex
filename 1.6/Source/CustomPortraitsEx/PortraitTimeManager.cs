using CustomPortraits;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    /// <summary>
    /// FPS計測、動画のステップモード移行判定、およびアニメーション画像のフレームスキップ管理を行うユーティリティ。
    /// </summary>
    public static class PortraitTimeManager
    {
        // ---- FPS計測 & ステップモード用 ----
        private static float _prev_frame_time = 0f;
        private static float _measured_fps = 60f;
        //private static int _low_fps_count = 0;
        private static readonly Queue<float> _fps_history = new Queue<float>();
        private static bool _low_fps_detected = false;

        private static int _high_fps_count = 0;
        //private static bool _high_fps_detected = false;

        public static bool LowFpsDetected => _low_fps_detected;

        // ---- アニメーションスキップ用 ----
        private static float _last_update_time = Time.realtimeSinceStartup;

        public static void ResetAllTimers()
        {
            _last_update_time = Time.realtimeSinceStartup;
            _prev_frame_time = 0f;
            //_low_fps_count = 0;
            _low_fps_detected = false;
            _high_fps_count = 0;
            _fps_history.Clear();
            //_high_fps_detected = false;
        }

        /// <summary>
        /// FPSを計測し、低FPS状態かどうかを判定する。
        /// 条件を満たした場合にステップモードへ移行させるコールバックを実行する。
        /// </summary>
        public static void UpdateFpsMeasurement(bool isVideoPlaying, bool isVideoStepMode, System.Action onTransitionToStepMode, System.Action onTransitionToPlayMode)
        {
            float now = Time.realtimeSinceStartup;
            if (_prev_frame_time > 0f)
            {
                float delta = now - _prev_frame_time;
                if (delta > 0f)
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


            //Log.Message($"[CustomPortraitsEx] Measured FPS: {_measured_fps:F2} {_prev_frame_time} {now} {PortraitCacheEx.Settings.video_step_mode_fps_threshold} {PortraitCacheEx.Settings.video_step_mode_trigger_count}");

            if (_measured_fps < PortraitCacheEx.Settings.video_step_mode_fps_threshold)
            {
                int low_fps_count = 0;

                foreach (float fps in _fps_history)
                {
                    if(fps < PortraitCacheEx.Settings.video_step_mode_fps_threshold)
                    {
                        low_fps_count++;
                    }
                }

                //_low_fps_count++;
                
                //_high_fps_detected = false;
                if (low_fps_count >= PortraitCacheEx.Settings.video_step_mode_trigger_count)
                {
                    _high_fps_count = 0;
                    //_fps_history.Clear();

                    ////if (Settings.Instance.debug)
                    //    Log.Message($"[CustomPortraitsEx] Low FPS detected: {_measured_fps:F2} fps. Transitioning to step mode. {PortraitCacheEx.Settings.video_step_mode_fps_threshold} {PortraitCacheEx.Settings.video_step_mode_trigger_count}");

                    _low_fps_detected = true;
                    // 現在ビデオ再生中でまだステップモードでなければ、動的に移行する
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
                    //_high_fps_detected = true;
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
                if (skipOnLag)
                {
                    // 現在の時刻と前フレームの時刻を計算して、frameIntervalSecondsに
                    // 収まらない場合はその分スキップする。
                    float delta = currentTime - _last_update_time;
                    skipCount = Mathf.FloorToInt(delta / frameIntervalSeconds);
                    // 余り分も次インターバルに含めるため、加算で更新
                    _last_update_time += skipCount * frameIntervalSeconds;
                }
                else
                {
                    _last_update_time = currentTime;
                }
                return true; // 次のポートレートへ進む
            }
            
            return false; // 進まない
        }
    }
}
