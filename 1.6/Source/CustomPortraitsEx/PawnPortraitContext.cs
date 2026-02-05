using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class PawnPortraitContext
    {
        public static Dictionary<string, float> ComposeImpactMap(Pawn pawn, out bool is_value_fetched)
        {
            Dictionary<string, float> impact_map = new Dictionary<string, float>();
            CollectMoodThoughtImpacts(pawn, impact_map);
            CollectInteractionImpacts(pawn, impact_map);
            AppendCombatContextImpact(pawn, impact_map);
            AppendDownedContext(pawn, impact_map);
            if (impact_map.Count > 0)
            {
                is_value_fetched = true;
            }
            else
            {
                is_value_fetched = false;
            }

            return impact_map;

        }

        // 以降特に監視する必要ない場合はここにメソッドを書いていく
        // 特定の値が必要ならSteady配下にクラスを作って

        // 心情
        public static void CollectMoodThoughtImpacts(Pawn pawn, Dictionary<string, float> impact_map)
        {
            // 別スレッドかどうか確認しておく ver1.6以降→要確認
            //Log.Message($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

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
                    if (impact_map.ContainsKey(need.LabelCap))
                    {
                        float weight1 = impact_map[need.LabelCap];
                        float weight2 = need.MoodOffset();
                        if (weight1 < weight2) impact_map[need.LabelCap] = weight2;
                    }
                    else
                    {
                        impact_map.Add(need.LabelCap, need.MoodOffset());
                    }
                }
                catch (Exception)
                {
                    //Log.Warning($"[PortraitsEx] WARN?(Processing will continue) Exception for need.LabelCap={need.LabelCap}: {e}");
                    impact_map.Add(need.LabelCap, 1.0f);
                }

            }
        }

        // インタラクト
        public static void CollectInteractionImpacts(Pawn pawn, Dictionary<string, float> impact_map)
        {
            // インタラクションで、ポーンがかかわったことを返却する
            PlayLogEntry_Interaction_ctor.CleanupExpiredAndExcessLogs();
            List<string> pawn_interaction_list = PlayLogEntry_Interaction_ctor.GetAllKeysByPawnTrimmedFinal(pawn);

            foreach (var key in pawn_interaction_list)
            {
                if (!impact_map.ContainsKey(key))
                {
                    impact_map[key] = 1.0f;
                }
            }

        }

        // 徴兵中
        public static void AppendCombatContextImpact(Pawn pawn, Dictionary<string, float> impact_map)
        {
            
            bool drafted = pawn.drafter?.Drafted ?? false;

            //Log.Message($"[PortraitsEx] CombatContext ==> drafted? {drafted}");
            if (drafted)
            {
                impact_map[PortraitContextKeys.COMBAT_CONTEXT] = 1.0f;
            }
        }

        // ダウン中
        public static void AppendDownedContext(Pawn pawn, Dictionary<string, float> impact_map)
        {
            bool downed = pawn?.health?.Downed ?? false;
            if (downed)
            {
                impact_map[PortraitContextKeys.STEADY_DOWNED] = 1.0f;
            }
        }
    }
}
