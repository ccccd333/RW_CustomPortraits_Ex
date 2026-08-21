using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    /// <summary>
    /// 特定の動画ファイルパスに紐づいた VideoPlayer + RenderTexture のキャッシュ単位。
    /// 一度ロードしたら Stop/Play で再利用し、URL の再設定（再デコード初期化）を避ける。
    /// </summary>
    public class CachedVideoPlayer
    {
        public readonly string absolute_path;

        private readonly GameObject _go;
        private readonly VideoPlayer _player;
        private readonly RenderTexture _rt;

        private bool _has_frame = false;
        private bool _is_video_ended = false;
        private bool _loop = true;
        private Texture2D _fallback_texture;

        public bool is_playing => _player != null && _player.isPlaying;
        public bool is_video_ended => _is_video_ended;

        public CachedVideoPlayer(string absolute_path, bool loop, Texture2D fallback_texture)
        {
            this.absolute_path = absolute_path;
            _loop = loop;
            _fallback_texture = fallback_texture;

            _go = new GameObject($"CustomPortraits_VideoPlayer_Cached_{System.IO.Path.GetFileNameWithoutExtension(absolute_path)}");
            Object.DontDestroyOnLoad(_go);

            _player = _go.AddComponent<VideoPlayer>();
            _player.playOnAwake      = false;
            _player.renderMode       = VideoRenderMode.RenderTexture;
            _player.audioOutputMode  = VideoAudioOutputMode.Direct;
            _player.SetDirectAudioVolume(0, PortraitCacheEx.Settings.video_audio_volume);
            _player.sendFrameReadyEvents = true;
            _player.frameReady       += OnFrameReady;
            _player.loopPointReached += OnLoopPointReached;
            _player.errorReceived    += OnErrorReceived;

            _rt = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
            _rt.Create();
            _player.targetTexture = _rt;

            // URL は生成時に一度だけセットする（以後は変えない）
            _player.url       = "file:///" + absolute_path.Replace("\\", "/");
            _player.isLooping = loop;
        }

        private void OnFrameReady(VideoPlayer vp, long frameIdx) => _has_frame = true;
        private void OnLoopPointReached(VideoPlayer vp)          => _is_video_ended = true;
        private void OnErrorReceived(VideoPlayer vp, string msg) => Log.Error($"[PortraitsEx] CachedVideoPlayer error ({absolute_path}): {msg}");

        /// <summary>再生開始。2回目以降は先頭シークして再スタート。</summary>
        public void Play()
        {
            _is_video_ended = false;
            _has_frame      = false;

            if (_player.isPlaying)
                _player.Stop();

            _player.frame     = 0;
            _player.isLooping = _loop;
            _player.Play();
        }

        /// <summary>停止（ポーン切り替え時など）。</summary>
        public void Stop()
        {
            if (_player != null && _player.isPlaying)
                _player.Stop();
            _has_frame      = false;
            _is_video_ended = false;
        }

        public void ResetEndedFlag() => _is_video_ended = false;

        public Texture GetTexture()
        {
            if (_player != null && _player.isPlaying && _has_frame)
                return _rt;
            if (_fallback_texture != null)
                return _fallback_texture;
            return _rt;
        }

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
            }
            if (_go != null)
                Object.Destroy(_go);
        }
    }
}
