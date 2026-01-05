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
        }

        public float display_duration { get; set; }
        public LogRetention recipient_log_retention { get; set; } = new LogRetention();
        public LogRetention initiator_log_retention { get; set; } = new LogRetention();
        public PortraitAnimationSettings portrait_animation { get; set; }
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
