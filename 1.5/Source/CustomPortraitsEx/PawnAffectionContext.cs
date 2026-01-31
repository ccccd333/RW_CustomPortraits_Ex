using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class PawnAffectionContext
    {
        public static Dictionary<string, float> ComposeAffectionImpactMap(Pawn pawn, out bool is_value_fetched)
        {
            Dictionary<string, float> affection_impact_map = new Dictionary<string, float>();
            CollectMoodThoughtImpacts(pawn, affection_impact_map);
            CollectInteractionImpacts(pawn, affection_impact_map);
            AppendCombatContextImpact(pawn, affection_impact_map);

            if (affection_impact_map.Count > 0)
            {
                is_value_fetched = true;
            }
            else
            {
                is_value_fetched = false;
            }

            return affection_impact_map;

        }

        // 心情
        public static void CollectMoodThoughtImpacts(Pawn pawn, Dictionary<string, float> affection_impact_map)
        {
            // 別スレッドかどうか確認しておく ver1.6以降→要確認
            //Log.Message($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            //is_value_fetched = false;
            //Dictionary<string, float> affection_impact_map = new Dictionary<string, float>();
            List<Thought> outThoughts = new List<Thought>();
            var thoughts = pawn.needs?.mood?.thoughts;

            // メカノイドなどは心情を持たない
            if (thoughts == null) { return; }

            thoughts.GetAllMoodThoughts(outThoughts);


            // 心情の文字と値のリスト化
            foreach (var need in outThoughts)
            {
                if (need == null || need.LabelCap == null)
                {
                    // 豪華な宿舎みたいにstage[0]がnullのものがあったりする。
                    // なのでこれはそれ用
                    //Log.Warning($"[PortraitsEx] WARN: need, LabelCap is null");
                    continue;
                }

                try
                {

                    // TODO:心情の値のほうで重みをつけるようにするかもしれない。
                    if (affection_impact_map.ContainsKey(need.LabelCap))
                    {
                        float weight1 = affection_impact_map[need.LabelCap];
                        float weight2 = need.MoodOffset();
                        if (weight1 < weight2) affection_impact_map[need.LabelCap] = weight2;
                    }
                    else
                    {
                        affection_impact_map.Add(need.LabelCap, need.MoodOffset());
                    }
                }
                catch (Exception)
                {
                    //Log.Warning($"[PortraitsEx] WARN?(Processing will continue) Exception for need.LabelCap={need.LabelCap}: {e}");
                    affection_impact_map.Add(need.LabelCap, 1.0f);
                }

            }
        }

        // インタラクト
        public static void CollectInteractionImpacts(Pawn pawn, Dictionary<string, float> affection_impact_map)
        {
            // インタラクションで、ポーンがかかわったことを返却する
            PlayLogEntry_Interaction_ctor.CleanupExpiredAndExcessLogs();
            List<string> pawn_interaction_list = PlayLogEntry_Interaction_ctor.GetAllKeysByPawnTrimmedFinal(pawn);

            foreach (var key in pawn_interaction_list)
            {
                if (!affection_impact_map.ContainsKey(key))
                {
                    affection_impact_map[key] = 1.0f;
                }
            }

        }

        // 徴兵中
        public static void AppendCombatContextImpact(Pawn pawn, Dictionary<string, float> existing_impact_map)
        {
            Dictionary<string, float> affection_impact_map = new Dictionary<string, float>();
            
            bool drafted = pawn.drafter?.Drafted ?? false;

            //Log.Message($"[PortraitsEx] CombatContext ==> drafted? {drafted}");

            affection_impact_map["CombatContext"] = 1.0f;

        }
    }
}
