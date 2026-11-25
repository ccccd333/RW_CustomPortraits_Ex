using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class VideoPlayerManager
    {
        private static VideoPlayerManager _instance;
        public static bool HasInstance => _instance != null;

        public static VideoPlayerManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new VideoPlayerManager();
                return _instance;
            }
        }

        public static void DestroyInstance()
        {
            if (_instance != null)
            {
                _instance.Cleanup();
                _instance = null;
            }
        }

        public static void StopIfActive()
        {
            if (_instance != null)
                _instance.Stop();
        }

        private GameObject _go;
        private VideoPlayer _player;
        private RenderTexture _rt;

        private string _currentPath = "";
        private bool _loadedUrlMatches = false;
        private bool _isVideoEnded = false;
        private bool _hasFrame = false;
        private bool _isStepMode = false;

        public bool HasFrame => _hasFrame;
        public long CurrentFrame => _player != null ? _player.frame : -1;

        public bool IsPlaying => _player != null && IsActive;
        public bool IsActive => !string.IsNullOrEmpty(_currentPath);
        public bool IsStepMode => _isStepMode;

        public bool IsVideoEnded
        {
            get
            {
                if (_isVideoEnded) return true;
                if (_player == null || _player.isLooping || !_hasFrame) return false;
                if (!_player.isPlaying || _isStepMode)
                {
                    if (_player.frameCount > 0 && (ulong)_player.frame >= _player.frameCount - 2) return true;
                    if (_player.length > 0 && _player.time >= _player.length - 0.1) return true;
                }
                return false;
            }
        }

        private Texture2D _fallbackTexture;

        private VideoPlayerManager()
        {
            _go = new GameObject("CustomPortraits_VideoPlayer");
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
        }

        private void OnFrameReady(VideoPlayer vp, long frameIdx)
        {
            _hasFrame = true;
        }

        private void OnLoopPointReached(VideoPlayer vp)
        {
            _isVideoEnded = true;
        }

        private void OnErrorReceived(VideoPlayer vp, string message)
        {
            Log.Error($"[PortraitsEx] VideoPlayer error: {message}");
        }

        public void SwitchClip(string absolutePath, bool loop, Texture2D fallbackTexture = null)
        {
            if (_currentPath == absolutePath && IsActive && _player != null && _player.isPlaying && !_isStepMode)
                return;

            bool sameLoadedUrl = (_loadedUrlMatches && _player.url == absolutePath && _player.isPrepared);

            _isVideoEnded    = false;
            _hasFrame        = false;
            _isStepMode      = false;
            _currentPath     = absolutePath;
            _fallbackTexture = fallbackTexture;

            if (sameLoadedUrl)
            {
                _player.time = 0.0;
            }
            else
            {
                _player.url = absolutePath;
                _loadedUrlMatches = true;
            }
            _player.isLooping = loop;
            _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
            _player.Play();
        }

        /// <summary>ステップモードでクリップ切り替え。音声ミュート、Pause状態で待機。</summary>
        public void SwitchClipStepMode(string absolutePath, bool loop, Texture2D fallbackTexture = null)
        {
            bool sameLoadedUrl = (_loadedUrlMatches && _player.url == absolutePath && _player.isPrepared);

            _isVideoEnded    = false;
            _hasFrame        = false;
            _isStepMode      = true;
            _currentPath     = absolutePath;
            _fallbackTexture = fallbackTexture;

            if (sameLoadedUrl)
            {
                _player.time = 0.0;
            }
            else
            {
                _player.url = absolutePath;
                _loadedUrlMatches = true;
            }
            _player.isLooping = loop;
            _player.SetDirectAudioVolume(0, 0f);
            _player.Play();
            _player.Pause();
        }

        /// <summary>再生中にステップモードへ動的に移行する。フレーム位置は維持。</summary>
        public void SwitchToStepMode()
        {
            _isStepMode = true;
            if (_player != null)
            {
                _player.Pause();
                _player.SetDirectAudioVolume(0, 0f);
                if (global::CustomPortraits.Settings.Instance != null && global::CustomPortraits.Settings.Instance.debug)
                {
                    Log.Message($"[PortraitsEx] [Diag] VideoPlayerManager.SwitchToStepMode: path={_currentPath}, frame={_player.frame}/{_player.frameCount}, prepared={_player.isPrepared}, double_step={PortraitCacheEx.Settings.double_step_forward}");
                }
            }
        }

        /// <summary>ステップモード中にデコード済みキューから1〜2フレーム進める。毎描画フレーム呼ぶ。</summary>
        public void StepForward()
        {
            if (_player != null && _isStepMode && !_isVideoEnded)
            {
                int steps = PortraitCacheEx.Settings.double_step_forward ? 2 : 1;
                _player.StepFrames(steps);
            }
        }

        public void SwitchToPlayMode()
        {
            _isStepMode = false;

            if (_player != null && IsActive && !_isVideoEnded)
            {
                _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
                _player.Play();
            }
        }

        public Texture GetTexture(Texture defaultFallback = null)
        {
            if (_player != null && _hasFrame && _rt != null)
                return _rt;

            if (_fallbackTexture != null)
                return _fallbackTexture;

            if (defaultFallback != null)
                return defaultFallback;

            return _rt;
        }

        public void Stop()
        {
            if (!Verse.UnityData.IsInMainThread)
            {
                Verse.LongEventHandler.ExecuteWhenFinished(() => Stop());
                return;
            }

            if (_player != null && IsActive)
            {
                _player.Pause();
                _player.SetDirectAudioVolume(0, 0f);
            }
            _currentPath     = "";
            _isVideoEnded    = false;
            _hasFrame        = false;
            _isStepMode      = false;
            _fallbackTexture = null;
        }

        public void ResetEndedFlag() => _isVideoEnded = false;

        public void Cleanup()
        {
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
                _rt = null;
            }
            if (_go != null)
            {
                Object.Destroy(_go);
                _go = null;
            }
            _currentPath      = "";
            _loadedUrlMatches = false;
            _isVideoEnded     = false;
            _hasFrame         = false;
            _isStepMode       = false;
            _fallbackTexture  = null;
        }
    }
}
