using Newtonsoft.Json;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public class PExSetting
    {
        public PExSetting()
        {
            // json読み込みで失敗したら適当に2秒
            display_duration = 2.0f;
            initiator_log_retention = new LogRetention();
            recipient_log_retention = new LogRetention();
            initiator_log_retention.seconds = 12.0f;
            initiator_log_retention.max_entries = 20;
            recipient_log_retention.seconds = 12.0f;
            recipient_log_retention.max_entries = 20;
            portrait_animation = new PortraitAnimationSettings();
            portrait_animation.portrait_skip_on_lag = false;
            portrait_animation.frame_interval_seconds = 0.1f;
            interrupt_fallback_to_steady = false;
        }

        public float display_duration { get; set; }
        public LogRetention recipient_log_retention { get; set; } = new LogRetention();
        public LogRetention initiator_log_retention { get; set; } = new LogRetention();
        public PortraitAnimationSettings portrait_animation { get; set; }
        public bool interrupt_fallback_to_steady { get; set; }

        /// <summary>
        /// VideoPlayer のプール上限数。0 の場合はキャッシュ無効。
        /// </summary>
        public int video_player_pool_limit { get; set; } = 0;

        private float _video_audio_volume = 1.0f;
        /// <summary>
        /// 動画の音声ボリューム (0.0 = ミュート, 1.0 = 最大)。
        /// </summary>
        public float video_audio_volume 
        { 
            get => _video_audio_volume; 
            set => _video_audio_volume = UnityEngine.Mathf.Clamp01(value); 
        }

        /// <summary>
        /// 動画をステップモード（コマ送り）に切り替えるFPSの閾値
        /// </summary>
        public float video_step_mode_fps_threshold { get; set; } = 12.0f;

        /// <summary>
        /// 閾値未満のFPSが何フレーム連続したらステップモードへ移行するか
        /// </summary>
        public int video_step_mode_trigger_count { get; set; } = 3;

        public int video_play_mode_trigger_count { get; set; } = 20;

        public int video_fps_history_size { get; set; } = 10;

        public bool double_step_forward { get; set; } = true;
    }

    public class LogRetention
    {
        public int max_entries { get; set; }
        public float seconds { get; set; }
    }

    public class PortraitAnimationSettings
    {
        public PortraitAnimationSettings()
        {
            frame_interval_seconds = 0.1f;
            portrait_skip_on_lag = true;
        }

        public float frame_interval_seconds { get; set; }

        public bool portrait_skip_on_lag { get; set; }
    }
}
