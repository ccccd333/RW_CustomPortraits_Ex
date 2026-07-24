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

        public bool IsPlaying => _player != null && _player.isPlaying;
        public bool IsActive => !string.IsNullOrEmpty(_currentPath);

        public bool IsVideoEnded => _isVideoEnded;

        private Texture2D _fallbackTexture;

        private VideoPlayerManager()
        {
            _go = new GameObject("CustomPortraits_VideoPlayer");
            Object.DontDestroyOnLoad(_go);

            _player = _go.AddComponent<VideoPlayer>();
            _player.playOnAwake = false;
            _player.renderMode  = VideoRenderMode.RenderTexture;
            _player.audioOutputMode = VideoAudioOutputMode.None;
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
            if (_currentPath == absolutePath && IsActive && _player != null && _player.isPlaying)
                return;

            _isVideoEnded    = false;
            _hasFrame        = false;
            _currentPath     = absolutePath;
            _fallbackTexture = fallbackTexture;

            _player.Stop();
            _player.url       = "file:///" + absolutePath.Replace("\\", "/");
            _player.isLooping = loop;
            _player.Play();
        }

        public Texture GetTexture()
        {
            if (_player != null && _player.isPlaying && _hasFrame)
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