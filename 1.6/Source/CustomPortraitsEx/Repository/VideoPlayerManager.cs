using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class VideoPlayerManager
    {
        private static VideoPlayerManager _instance;
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
        private bool _isVideoEnded = false;
        private bool _hasFrame = false;
        private bool _isStepMode = false;

        public bool IsPlaying => _player != null && (_player.isPlaying || _isStepMode);
        public bool IsActive => !string.IsNullOrEmpty(_currentPath);
        public bool IsStepMode => _isStepMode;

        public bool IsVideoEnded
        {
            get
            {
                if (_isVideoEnded) return true;
                if (_player == null || _player.isLooping || !_hasFrame) return false;
                // ステップモードでは isPlaying が常に false なのでフレーム位置で判定
                if (!_player.isPlaying || _isStepMode)
                {
                    // Unityイベント(loopPointReached)がラグ等で不発した場合のフォールバック
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
            _player.playOnAwake = false;
            _player.renderMode  = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.Direct;
            _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
            _player.sendFrameReadyEvents = true;
            _player.frameReady       += OnFrameReady;
            _player.loopPointReached += OnLoopPointReached;
            _player.errorReceived    += OnErrorReceived;

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

            _isVideoEnded    = false;
            _hasFrame        = false;
            _isStepMode      = false;
            _currentPath     = absolutePath;
            _fallbackTexture = fallbackTexture;

            _player.Stop();
            _player.url       = "file:///" + absolutePath.Replace("\\", "/");
            _player.isLooping = loop;
            _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
            _player.Play();
        }

        /// <summary>ステップモードでクリップ切り替え。音声ミュート、Pause状態で待機。</summary>
        public void SwitchClipStepMode(string absolutePath, bool loop, Texture2D fallbackTexture = null)
        {
            _isVideoEnded    = false;
            _hasFrame        = false;
            _isStepMode      = true;
            _currentPath     = absolutePath;
            _fallbackTexture = fallbackTexture;

            _player.Stop();
            _player.url       = "file:///" + absolutePath.Replace("\\", "/");
            _player.isLooping = loop;
            _player.SetDirectAudioVolume(0, 0f);
            _player.Play();
            _player.Pause();
        }

        /// <summary>再生中にステップモードへ動的に移行する。フレーム位置は維持。</summary>
        public void SwitchToStepMode()
        {
            _isStepMode = true;
            if (_player != null && _player.isPlaying)
                _player.Pause();
            if (_player != null)
                _player.SetDirectAudioVolume(0, 0f);
        }

        /// <summary>ステップモード中に1フレーム進める。毎フレーム呼ぶ。</summary>
        public void StepForward()
        {
            if (_player != null && _isStepMode && !_isVideoEnded)
            {
                _player.StepForward();
                if (PortraitCacheEx.Settings.double_step_forward)
                {
                    _player.StepForward();
                }
            }
        }

        public void SwitchToPlayMode()
        {
            _isStepMode = false;

            if (_player != null && !_isVideoEnded)
            {
                _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
                _player.Play();
            }
        }

        public Texture GetTexture()
        {
            if (_player != null && (_player.isPlaying || (_isStepMode && !_isVideoEnded)) && _hasFrame)
                return _rt;
            
            if (_fallbackTexture != null)
                return _fallbackTexture;

            return _rt;
        }

        public void Stop()
        {
            if (_player != null && IsPlaying)
                _player.Stop();
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
            _currentPath  = "";
            _isVideoEnded = false;
            _hasFrame     = false;
        }
    }
}