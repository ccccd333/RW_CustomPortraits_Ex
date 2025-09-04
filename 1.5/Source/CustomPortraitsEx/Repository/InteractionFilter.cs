using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class InteractionFilter
    {
        public InteractionFilter() { }
        public InteractionFilter(InteractionFilter intf)
        {
            is_recipient = intf.is_recipient;
            is_initiator = intf.is_initiator;
            matched_initiator_key = intf.matched_initiator_key;
            matched_recipient_key = intf.matched_recipient_key;
            cache_duration_seconds = intf.cache_duration_seconds;
            interaction_name = intf.interaction_name;
        }

        public bool IsCacheDurationDifferent(InteractionFilter intf)
        {
            if (intf.cache_duration_seconds != cache_duration_seconds)
            {
                return true;
            }

            // 差分なし
            return false;
        }

        public bool is_recipient = false;
        public bool is_initiator = false;
        public string matched_recipient_key = "";
        public string matched_initiator_key = "";
        public float cache_duration_seconds = 12.0f;
        public string interaction_name = "";
    }
}
