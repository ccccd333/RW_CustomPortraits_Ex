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
        public bool is_recipient = false;
        public bool is_initiator = false;
        public string matched_recipient_key = "";
        public string matched_initiator_key = "";
        public float cache_duration_seconds = 12.0f;
    }
}
