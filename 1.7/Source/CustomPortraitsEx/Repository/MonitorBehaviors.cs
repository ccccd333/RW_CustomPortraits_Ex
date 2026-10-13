using Newtonsoft.Json.Linq;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static Foxy.CustomPortraits.CustomPortraitsEx.Repository.MBPainIncrease;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{


    public class MonitorBehaviors
    {
        public void LoadFromJson(JToken monitor_behaviors_token)
        {
            foreach (var iv in monitor_behaviors_token)
            {
                var prop = (JProperty)iv;
                string key = prop.Name;

                JToken value = prop.Value;

                if (key == PortraitContextKeys.PAIN_INCREASE)
                {
                    pain_increase.LoadFromJson(value);
                }
                else if (key == PortraitContextKeys.DOWNED)
                {
                    downed.LoadFromJson(value);
                }
            }
        }

        // PainIncrease
        public MBPainIncrease pain_increase = new MBPainIncrease();

        // Downed
        public MBDowned downed = new MBDowned();
    }

    interface IMonitorBehavior
    {
        void LoadFromJson(JToken n);
    }

    public class MBPainIncrease : IMonitorBehavior
    {
        public enum TriggerMode
        {
            Continuous,
            OnEnter,
            Threshold
        }

        public void LoadFromJson(JToken n)
        {
            foreach (var iv in n)
            {
                var prop = (JProperty)iv;
                string key = prop.Name;

                JToken value = prop.Value;

                if (key == "trigger_mode" && value is JValue tm)
                {
                    if (Enum.TryParse(tm.Value<string>(), out TriggerMode parsed))
                    {
                        trigger_mode = parsed;
                    }
                }
                else if (key == "delta_threshold")
                {
                    if (value is JValue dt)
                    {
                        delta_threshold = dt.Value<float>();
                    }
                }
            }

            //Log.Message($"[PortraitsEx] MBPainIncrease delta_threshold {delta_threshold} trigger_mode {trigger_mode}");
        }

        public TriggerMode trigger_mode = TriggerMode.Threshold;
        public float delta_threshold = 0.0f;
    }

    public class MBDowned : IMonitorBehavior
    {
        public bool trigger_on_enter = false;

        public void LoadFromJson(JToken n)
        {
            foreach (var iv in n)
            {
                var prop = (JProperty)iv;
                string key = prop.Name;

                JToken value = prop.Value;

                if (key == "trigger_on_enter")
                {
                    if (value is JValue toe)
                    {
                        trigger_on_enter = toe.Value<bool>();
                    }
                }
            }

            //Log.Message($"[PortraitsEx] MBDowned trigger_on_enter {trigger_on_enter}");
        }
    }

   

}
