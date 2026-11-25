using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    public static class PawnPortraitContext
    {
        [ThreadStatic]
        private static List<Thought> _thought_buffer;

        public static Dictionary<string, float> ComposeImpactMap(Pawn pawn, out bool is_value_fetched)
        {
            Dictionary<string, float> impact_map = new Dictionary<string, float>();
            CollectMoodThoughtImpacts(pawn, impact_map);
            CollectInteractionImpacts(pawn, impact_map);
            AppendCombatContextImpact(pawn, impact_map);
            AppendDownedContext(pawn, impact_map);
            is_value_fetched = impact_map.Count > 0;
            return impact_map;
        }

        // 以降特に監視する必要ない場合はここにメソッドを書いていく
        // 特定の値が必要ならSteady配下にクラスを作って

        // 心情
        public static void CollectMoodThoughtImpacts(Pawn pawn, Dictionary<string, float> impact_map)
        {
            var thoughts = pawn?.needs?.mood?.thoughts;
            // メカノイドなどは心情を持たない
            if (thoughts == null)
                return;

            List<Thought> outThoughts = _thought_buffer ?? (_thought_buffer = new List<Thought>(32));
            outThoughts.Clear();

            try
            {
                thoughts.GetAllMoodThoughts(outThoughts);

                // 心情の文字と値のリスト化
                for (int i = 0; i < outThoughts.Count; i++)
                {
                    Thought need = outThoughts[i];
                    if (need == null)
                        continue;

                    string label;
                    try
                    {
                        label = need.LabelCap;
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    // 豪華な宿舎みたいにstage[0]がnullのものがあったりする。なのでこれはそれ用
                    if (string.IsNullOrEmpty(label))
                        continue;

                    try
                    {
                        // TODO:心情の値のほうで重みをつけるようにするかもしれない。
                        float offset = need.MoodOffset();
                        if (impact_map.TryGetValue(label, out float existing))
                        {
                            if (existing < offset)
                                impact_map[label] = offset;
                        }
                        else
                        {
                            impact_map.Add(label, offset);
                        }
                    }
                    catch (Exception)
                    {
                        if (!impact_map.ContainsKey(label))
                            impact_map.Add(label, 1.0f);
                    }
                }
            }
            finally
            {
                outThoughts.Clear();
            }
        }

        // インタラクト
        public static void CollectInteractionImpacts(Pawn pawn, Dictionary<string, float> impact_map)
        {
            PlayLogEntry_Interaction_ctor.CollectActiveKeysIntoImpactMap(pawn, impact_map);
        }

        // 徴兵中
        public static void AppendCombatContextImpact(Pawn pawn, Dictionary<string, float> impact_map)
        {
            if (pawn?.drafter?.Drafted ?? false)
            {
                impact_map[PortraitContextKeys.COMBAT_CONTEXT] = 1.0f;
            }
        }

        // ダウン中
        public static void AppendDownedContext(Pawn pawn, Dictionary<string, float> impact_map)
        {
            if (pawn?.health?.Downed ?? false)
            {
                impact_map[PortraitContextKeys.STEADY_DOWNED] = 1.0f;
            }
        }
    }
}
