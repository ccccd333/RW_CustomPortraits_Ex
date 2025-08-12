using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class InteractionSelectionMap
    {
        public Dictionary<string, InteractionFilter> InteractionFilter = new Dictionary<string, InteractionFilter>();
        public Dictionary<string, Regex> intf_regex_cache = new Dictionary<string, Regex>();
    }
}
