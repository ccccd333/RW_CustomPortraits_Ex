using CustomPortraits;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository
{
    public class Refs
    {
        public Dictionary<string, Textures> txs = new Dictionary<string, Textures>();
        /// <summary>mp4 動画エントリ。textures の代わりに video キーで JSON 定義されたもの。</summary>
        public Dictionary<string, VideoEntry> videos = new Dictionary<string, VideoEntry>();
        /// <summary>生成済みの動画プレイヤーキャッシュ</summary>
        public Dictionary<string, CachedVideoPlayer> cached_videos = new Dictionary<string, CachedVideoPlayer>();
        public Dictionary<string, GroupPatternEntry> group_filter = new Dictionary<string, GroupPatternEntry>();
        public Dictionary<string, PriorityWeights> priority_weights = new Dictionary<string, PriorityWeights>();
        public PortraitInterrupt interrupt = new PortraitInterrupt();
        public RepeatRules repeat_rules = new RepeatRules();

        public List<string> priority_weight_order = new List<string>();
        public string fallback_mood = "";
        public string fallback_mood_on_death = "";

        public Dictionary<string, IPatternMatcher> g_regex_cache = new Dictionary<string, IPatternMatcher>();

        public bool MatchDictKeysByRegex(string input, out string access_key)
        {
            if (!string.IsNullOrEmpty(input) && txs.ContainsKey(input))
            {
                if (Settings.Instance.debug)
                {
                    Log.Message($"[PortraitsEx] pic ==> key: {input} input: {input}");
                }
                access_key = input;
                return true;
            }

            access_key = "";
            return false;
        }

        /// <summary>
        /// videos 辞書のキーと input を O(1) で照合する。
        /// </summary>
        public bool MatchVideoKey(string input, out string access_key)
        {
            if (!string.IsNullOrEmpty(input) && videos.ContainsKey(input))
            {
                if (Settings.Instance.debug)
                {
                    Log.Message($"[PortraitsEx] video ==> key: {input} input: {input}");
                }
                access_key = input;
                return true;
            }

            access_key = "";
            return false;
        }

        public virtual bool contain(string key) { return false; }
    }
}
