using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public enum MonitorType
    {
        PainIncrease,
        Downed,
        Count
    }

    public class PortraitInterrupt
    {
        public PortraitInterrupt()
        {
            Disable();
        }

        public void Disable()
        {
            // 処理途中で続行不能な場合
            int mc = (int)MonitorType.Count;
            for (int c = 0; c < mc; c++)
            {
                enabled_monitors[c] = false;
            }

            interrupt_enabled = false;
        }

        public bool interrupt_enabled = false;
        public bool[] enabled_monitors = new bool[(int)MonitorType.Count];

        public MonitorBehaviors monitor_behaviors = new MonitorBehaviors();

        public Dictionary<string, string> group_filter = new Dictionary<string, string>();
        public Dictionary<string, PriorityWeights> priority_weights = new Dictionary<string, PriorityWeights>();
        public List<string> priority_weight_order = new List<string>();
    }
}
