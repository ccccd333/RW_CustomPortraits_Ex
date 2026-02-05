using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class PortraitContextKeys
    {
        // Interrupt contexts
        public const string PAIN_INCREASE = "PainIncrease";
        public const string DOWNED = "Downed";
        
        // Steady contexts
        public const string COMBAT_CONTEXT = "CombatContext";
        public const string STEADY_DOWNED = "SteadyDowned";
    }
}
