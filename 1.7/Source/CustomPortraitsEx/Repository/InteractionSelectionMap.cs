using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using System.Collections.Generic;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class InteractionSelectionMap
    {
        public Dictionary<string, InteractionFilter> InteractionFilter = new Dictionary<string, InteractionFilter>();
        public Dictionary<string, IPatternMatcher> intf_regex_cache = new Dictionary<string, IPatternMatcher>();
    }
}
