using CustomPortraits;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class Refs
    {
        public Dictionary<string, Textures> txs = new Dictionary<string, Textures>();
        public Dictionary<string, GroupPatternEntry> group_filter = new Dictionary<string, GroupPatternEntry>();
        public Dictionary<string, PriorityWeights> priority_weights = new Dictionary<string, PriorityWeights>();
        public PortraitInterrupt interrupt = new PortraitInterrupt();

        public List<string> priority_weight_order = new List<string>();
        public string fallback_mood = "";
        public string fallback_mood_on_death = "";

        //public Dictionary<string, Regex> txs_regex_cache = new Dictionary<string, Regex>();
        public Dictionary<string, IPatternMatcher> g_regex_cache = new Dictionary<string, IPatternMatcher>();
        //public Dictionary<string, Regex> pw_regex_cache = new Dictionary<string, Regex>();

        public bool MatchDictKeysByRegex(string input, out string access_key)
        {
            access_key = "";

            foreach (var tx in txs)
            {
                ////Log.Message($"[PortraitsEx] MatchDictKeysByRegex key: {tx.Key} input: {input}");
                //if (txs_regex_cache.ContainsKey(tx.Key))
                //{
                //    var reg = txs_regex_cache[tx.Key];
                //    if (reg.IsMatch(input))
                //    {
                //        //Log.Message($"[PortraitsEx] MatchDictKeysByRegex pic ==> key: {tx.Key} input: {input}");
                //        access_key = tx.Key;
                //        return true;
                //    }
                //}
                //else
                {
                    if (tx.Key == input)
                    {
                        access_key = tx.Key;
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool contain(string key) { return false; }
    }
}
