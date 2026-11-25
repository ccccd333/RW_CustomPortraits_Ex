using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    /// <summary>
    /// 特定の動画ファイルパスに紐づいた VideoPlayer + RenderTexture のキャッシュ単位。
    /// RimWorld 1.7 (Waylith.VideoDecode) では Stop() を呼ぶとデコーダスレッドと GPU フレームプールが破棄されるため、
    /// 一時停止時は Pause() でデコーダを維持し、Cleanup() 時のみ破棄する。
    /// </summary>
    public class CachedVideoPlayer
    {
        public readonly string absolute_path;

        private readonly GameObject _go;
        private readonly VideoPlayer _player;
        private readonly RenderTexture _rt;

        private bool _is_active = false;
        private bool _has_frame = false;
        private bool _is_video_ended = false;
        private bool _loop = true;
        private Texture2D _fallback_texture;
        private bool _is_step_mode = false;

        public bool HasFrame => _has_frame;
        public long CurrentFrame => _player != null ? _player.frame : -1;

        public bool is_playing => _player != null && _is_active;
        public bool is_step_mode => _is_step_mode;
        public bool is_video_ended
        {
            get
            {
                if (_is_video_ended) return true;
                if (_player == null || _loop || !_has_frame) return false;
                if (!_player.isPlaying || _is_step_mode)
                {
                    if (_player.frameCount > 0 && (ulong)_player.frame >= _player.frameCount - 2) return true;
                    if (_player.length > 0 && _player.time >= _player.length - 0.1) return true;
                }
                return false;
            }
        }

        public CachedVideoPlayer(string absolute_path, bool loop, Texture2D fallback_texture)
        {
            this.absolute_path = absolute_path;
            _loop = loop;
            _fallback_texture = fallback_texture;

            _go = new GameObject($"CustomPortraits_VideoPlayer_Cached_{System.IO.Path.GetFileNameWithoutExtension(absolute_path)}");
            Object.DontDestroyOnLoad(_go);

            _player = _go.AddComponent<VideoPlayer>();
            _player.playOnAwake          = false;
            _player.renderMode           = VideoRenderMode.RenderTexture;
            _player.audioOutputMode      = VideoAudioOutputMode.Direct;
            _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
            _player.sendFrameReadyEvents = true;
            _player.frameReady           += OnFrameReady;
            _player.loopPointReached     += OnLoopPointReached;
            _player.errorReceived        += OnErrorReceived;

            _rt = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
            _rt.Create();
            _player.targetTexture = _rt;

            // 1.7 の UnityStubs は生のファイルパスをそのまま受け付ける（# や % を含むパスでも欠損しない）
            _player.url       = absolute_path;
            _player.isLooping = loop;
        }

        private void OnFrameReady(VideoPlayer vp, long frameIdx) => _has_frame = true;
        private void OnLoopPointReached(VideoPlayer vp)          => _is_video_ended = true;
        private void OnErrorReceived(VideoPlayer vp, string msg) => Log.Error($"[PortraitsEx] CachedVideoPlayer error ({absolute_path}): {msg}");

        /// <summary>通常再生開始。2回目以降はデコーダを維持したまま先頭シークして再スタート。</summary>
        public void Play()
        {
            _is_video_ended = false;
            _has_frame      = false;
            _is_step_mode   = false;
            _is_active      = true;

            _player.isLooping = _loop;
            _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
            if (_player.isPrepared)
            {
                _player.time = 0.0;
            }
            _player.Play();
        }

        /// <summary>ステップモードで再生準備。音声ミュート、Pause状態で待機。</summary>
        public void PrepareStepMode()
        {
            _is_video_ended = false;
            _has_frame      = false;
            _is_step_mode   = true;
            _is_active      = true;

            _player.isLooping = _loop;
            _player.SetDirectAudioVolume(0, 0f);
            if (_player.isPrepared)
            {
                _player.time = 0.0;
            }
            _player.Play();
            _player.Pause();
        }

        /// <summary>再生中にステップモードへ動的に移行する。フレーム位置は維持。</summary>
        public void SwitchToStepMode()
        {
            _is_step_mode = true;
            if (_player != null)
            {
                _player.Pause();
                _player.SetDirectAudioVolume(0, 0f);
                if (global::CustomPortraits.Settings.Instance != null && global::CustomPortraits.Settings.Instance.debug)
                {
                    Log.Message($"[PortraitsEx] [Diag] CachedVideoPlayer.SwitchToStepMode: path={absolute_path}, frame={_player.frame}/{_player.frameCount}, prepared={_player.isPrepared}, double_step={PortraitCacheEx.Settings.double_step_forward}");
                }
            }
        }

        /// <summary>ステップモード中にデコード済みキューから1〜2フレーム進める。毎描画フレーム呼ぶ。</summary>
        public void StepForward()
        {
            if (_player != null && _is_step_mode && !_is_video_ended)
            {
                int steps = PortraitCacheEx.Settings.double_step_forward ? 2 : 1;
                _player.StepFrames(steps);
            }
        }

        public void SwitchToPlayMode()
        {
            _is_step_mode = false;

            if (_player != null && _is_active && !_is_video_ended)
            {
                _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
                _player.Play();
            }
        }

        /// <summary>一時停止（ポーン切り替え・コンテキスト切り替え時）。デコーダプールは維持する。</summary>
        public void Stop()
        {
            if (!Verse.UnityData.IsInMainThread)
            {
                Verse.LongEventHandler.ExecuteWhenFinished(() => Stop());
                return;
            }

            if (_player != null && _is_active)
            {
                _player.Pause();
                _player.SetDirectAudioVolume(0, 0f);
            }
            _is_active      = false;
            _has_frame      = false;
            _is_video_ended = false;
            _is_step_mode   = false;
        }

        public void ResetEndedFlag() => _is_video_ended = false;

        public Texture GetTexture(Texture defaultFallback = null)
        {
            if (_player != null && _has_frame && _rt != null)
                return _rt;
            if (_fallback_texture != null)
                return _fallback_texture;
            if (defaultFallback != null)
                return defaultFallback;
            return _rt;
        }

        public void Cleanup()
        {
            _is_active = false;
            if (_player != null)
            {
                _player.Stop();
                _player.loopPointReached -= OnLoopPointReached;
                _player.errorReceived    -= OnErrorReceived;
                _player.frameReady       -= OnFrameReady;
            }
            if (_rt != null)
            {
                _rt.Release();
                Object.Destroy(_rt);
            }
            if (_go != null)
                Object.Destroy(_go);
        }
    }
}
