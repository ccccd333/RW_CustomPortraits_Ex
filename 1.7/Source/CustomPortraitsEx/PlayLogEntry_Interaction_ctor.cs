using CustomPortraits;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    [HarmonyPatch(typeof(PlayLog), nameof(PlayLog.Add))]
    public static class PlayLogEntry_Interaction_ctor
    {
        private class CachedInteraction
        {
            public string dedup_key;
            public string resolved_key;
            public float cached_at;
            public float expire_at;
        }

        // PlayLog.Add(LogEntry) では PlayLogEntry_Interaction 内の protected フィールドに直接アクセスできないため、
        // Harmony の FieldRefAccess（起動時に1回だけ生成される高速な直接フィールド参照デリゲート）を使って
        // intDef（社交の種類）・initiator（話しかけたポーン）・recipient（話しかけられたポーン）を取り出す。
        private static readonly AccessTools.FieldRef<PlayLogEntry_Interaction, InteractionDef> IntDefRef =
            AccessTools.FieldRefAccess<PlayLogEntry_Interaction, InteractionDef>("intDef");
        private static readonly AccessTools.FieldRef<PlayLogEntry_Interaction, Pawn> InitiatorRef =
            AccessTools.FieldRefAccess<PlayLogEntry_Interaction, Pawn>("initiator");
        private static readonly AccessTools.FieldRef<PlayLogEntry_Interaction, Pawn> RecipientRef =
            AccessTools.FieldRefAccess<PlayLogEntry_Interaction, Pawn>("recipient");

        // キーは pawn.thingIDNumber（Pawnインスタンス自体は保持しない）
        private static readonly Dictionary<int, List<CachedInteraction>> RecipientLogDict = new Dictionary<int, List<CachedInteraction>>();
        private static int RecipientTotalCount = 0;

        private static readonly Dictionary<int, List<CachedInteraction>> InitiatorLogDict = new Dictionary<int, List<CachedInteraction>>();
        private static int InitiatorTotalCount = 0;

        private static readonly List<int> EmptyPawnIdsBuffer = new List<int>();

        public static void Postfix(LogEntry entry)
        {
            // セーブデータ(XML)にはInteractで調べるとPlayLogEntry_Interactionのみが引っかかる
            // Pawn_InteractionsTrackerにlastInteractionDefとあるが、他mod（RimTalk等）も含め実プレイ中の社交はPlayLog.Addを通る。
            // コンストラクタをフックするとRimTalkのセーブ時変換（new PlayLogEntry_Interaction）まで拾ってしまうため、PlayLog.Add側で監視する。
            try
            {
                if (!PortraitCacheEx.IsAvailable)
                    return;

                // セーブ・ロード中や非プレイ中は無視する
                if (Current.ProgramState != ProgramState.Playing || Scribe.mode != LoadSaveMode.Inactive)
                    return;

                if (!(entry is PlayLogEntry_Interaction interaction))
                    return;

                InteractionDef intDef = IntDefRef(interaction);
                if (intDef == null)
                    return;

                string label_cap = intDef.LabelCap;
                if (string.IsNullOrEmpty(label_cap))
                    return;

                Pawn initiator = InitiatorRef(interaction);
                Pawn recipient = RecipientRef(interaction);

                if (Settings.Instance.debug)
                {
                    Log.Message($"[PortraitsEx] InteractionDef {label_cap} initiator {GetPawnDebugName(initiator)} recipient {GetPawnDebugName(recipient)}");
                }

                // ポートレート切替機能を利用していないポーン同士の会話などははじく
                if (!HasCustomPortrait(initiator) && !HasCustomPortrait(recipient))
                {
                    if (Settings.Instance.debug)
                    {
                        Log.Message($"[PortraitsEx] Skip: no portrait name (Inspector/Default) {label_cap} initiator {GetPawnDebugName(initiator)} recipient {GetPawnDebugName(recipient)}");
                    }
                    return;
                }

                CleanupExpiredAndExcessLogs();

                var ismap = PortraitCacheEx.InteractionSelectionMap;
                foreach (var intef in ismap.InteractionFilter)
                {
                    var filter = intef.Value;
                    if (!filter.is_recipient && !filter.is_initiator)
                        continue;

                    bool matched;
                    if (ismap.intf_regex_cache != null && ismap.intf_regex_cache.TryGetValue(intef.Key, out var matcher))
                    {
                        matched = matcher.IsMatch(label_cap);
                    }
                    else
                    {
                        matched = (label_cap == intef.Key);
                    }

                    if (matched)
                    {
                        PushDict(label_cap, filter, initiator, recipient);
                    }
                }
            }
            catch (Exception e)
            {
                try
                {
                    Log.Message($"[PortraitsEx] Please notify me if this error log is generated. wt ==> {e.Message}");
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool HasCustomPortrait(Pawn pawn)
        {
            if (pawn?.RaceProps == null)
                return false;
            return pawn.HasPortraitName(null) || pawn.HasPortraitName(PortraitPosition.Inspector);
        }

        private static string GetPawnDebugName(Pawn pawn)
        {
            if (pawn == null) return "null";
            return pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelShort;
        }

        public static void Clear()
        {
            InitiatorLogDict.Clear();
            InitiatorTotalCount = 0;
            RecipientLogDict.Clear();
            RecipientTotalCount = 0;
            EmptyPawnIdsBuffer.Clear();
        }

        public static void CleanupExpiredAndExcessLogs()
        {
            float now = Time.realtimeSinceStartup;
            if (InitiatorTotalCount > 0)
            {
                CleanupDict(InitiatorLogDict, ref InitiatorTotalCount, now, PortraitCacheEx.Settings.initiator_log_retention.max_entries);
            }
            if (RecipientTotalCount > 0)
            {
                CleanupDict(RecipientLogDict, ref RecipientTotalCount, now, PortraitCacheEx.Settings.recipient_log_retention.max_entries);
            }
        }

        public static List<string> GetAllKeysByPawnTrimmedFinal(Pawn targetPawn)
        {
            var result = new List<string>();
            if (targetPawn == null)
                return result;

            var seen = new HashSet<string>();
            float now = Time.realtimeSinceStartup;
            int pawn_id = targetPawn.thingIDNumber;

            CollectKeysFromDict(RecipientLogDict, ref RecipientTotalCount, pawn_id, now, seen, result);
            CollectKeysFromDict(InitiatorLogDict, ref InitiatorTotalCount, pawn_id, now, seen, result);
            return result;
        }

        public static void CollectActiveKeysIntoImpactMap(Pawn targetPawn, Dictionary<string, float> impact_map)
        {
            if (targetPawn == null || impact_map == null)
                return;

            float now = Time.realtimeSinceStartup;
            int pawn_id = targetPawn.thingIDNumber;

            CollectKeysIntoImpactMap(RecipientLogDict, ref RecipientTotalCount, pawn_id, now, impact_map);
            CollectKeysIntoImpactMap(InitiatorLogDict, ref InitiatorTotalCount, pawn_id, now, impact_map);
        }

        private static void PushDict(string label_cap, InteractionFilter filter, Pawn initiator, Pawn recipient)
        {
            float cache_duration_seconds = filter.cache_duration_seconds;
            if (cache_duration_seconds <= 0.0001f)
                return;

            if (filter.is_initiator && initiator != null)
            {
                string resolved = !string.IsNullOrEmpty(filter.matched_initiator_key) ? filter.matched_initiator_key : label_cap;
                AddOrUpdate(
                    InitiatorLogDict,
                    ref InitiatorTotalCount,
                    initiator.thingIDNumber,
                    label_cap,
                    resolved,
                    cache_duration_seconds,
                    PortraitCacheEx.Settings.initiator_log_retention.max_entries);
            }

            if (filter.is_recipient && recipient != null)
            {
                string resolved = !string.IsNullOrEmpty(filter.matched_recipient_key) ? filter.matched_recipient_key : label_cap;
                AddOrUpdate(
                    RecipientLogDict,
                    ref RecipientTotalCount,
                    recipient.thingIDNumber,
                    label_cap,
                    resolved,
                    cache_duration_seconds,
                    PortraitCacheEx.Settings.recipient_log_retention.max_entries);
            }
        }

        private static void AddOrUpdate(
            Dictionary<int, List<CachedInteraction>> dict,
            ref int total_count,
            int pawn_id,
            string dedup_key,
            string resolved_key,
            float duration_seconds,
            int max_entries)
        {
            if (max_entries <= 0)
                return;

            float now = Time.realtimeSinceStartup;
            float expire_at = now + duration_seconds;

            if (dict.TryGetValue(pawn_id, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item.dedup_key == dedup_key && item.resolved_key == resolved_key)
                    {
                        item.cached_at = now;
                        item.expire_at = expire_at;
                        return;
                    }
                }
            }

            while (total_count >= max_entries)
            {
                if (!EvictOldest(dict, ref total_count))
                    break;
            }

            if (list == null)
            {
                list = new List<CachedInteraction>(4);
                dict[pawn_id] = list;
            }

            list.Add(new CachedInteraction
            {
                dedup_key = dedup_key,
                resolved_key = resolved_key,
                cached_at = now,
                expire_at = expire_at
            });
            total_count++;
        }

        private static void CollectKeysFromDict(
            Dictionary<int, List<CachedInteraction>> dict,
            ref int total_count,
            int pawn_id,
            float now,
            HashSet<string> seen,
            List<string> result)
        {
            if (!dict.TryGetValue(pawn_id, out var list) || list.Count == 0)
                return;

            PurgeExpiredForPawn(dict, ref total_count, pawn_id, list, now);
            for (int i = 0; i < list.Count; i++)
            {
                string key = list[i].resolved_key;
                if (seen.Add(key))
                {
                    result.Add(key);
                }
            }
        }

        private static void CollectKeysIntoImpactMap(
            Dictionary<int, List<CachedInteraction>> dict,
            ref int total_count,
            int pawn_id,
            float now,
            Dictionary<string, float> impact_map)
        {
            if (!dict.TryGetValue(pawn_id, out var list) || list.Count == 0)
                return;

            PurgeExpiredForPawn(dict, ref total_count, pawn_id, list, now);
            for (int i = 0; i < list.Count; i++)
            {
                string key = list[i].resolved_key;
                if (!impact_map.ContainsKey(key))
                {
                    impact_map[key] = 1.0f;
                }
            }
        }

        private static void PurgeExpiredForPawn(
            Dictionary<int, List<CachedInteraction>> dict,
            ref int total_count,
            int pawn_id,
            List<CachedInteraction> list,
            float now)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (now >= list[i].expire_at)
                {
                    list.RemoveAt(i);
                    total_count--;
                }
            }
            if (list.Count == 0)
            {
                dict.Remove(pawn_id);
            }
        }

        private static void CleanupDict(Dictionary<int, List<CachedInteraction>> dict, ref int total_count, float now, int max_entries)
        {
            EmptyPawnIdsBuffer.Clear();
            foreach (var kvp in dict)
            {
                var list = kvp.Value;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (now >= list[i].expire_at)
                    {
                        list.RemoveAt(i);
                        total_count--;
                    }
                }
                if (list.Count == 0)
                {
                    EmptyPawnIdsBuffer.Add(kvp.Key);
                }
            }

            for (int i = 0; i < EmptyPawnIdsBuffer.Count; i++)
            {
                dict.Remove(EmptyPawnIdsBuffer[i]);
            }
            EmptyPawnIdsBuffer.Clear();

            while (total_count > max_entries && max_entries >= 0)
            {
                if (!EvictOldest(dict, ref total_count))
                    break;
            }
        }

        private static bool EvictOldest(Dictionary<int, List<CachedInteraction>> dict, ref int total_count)
        {
            int oldest_pawn_id = 0;
            int oldest_index = -1;
            float oldest_cached_at = float.MaxValue;

            foreach (var kvp in dict)
            {
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].cached_at < oldest_cached_at)
                    {
                        oldest_cached_at = list[i].cached_at;
                        oldest_pawn_id = kvp.Key;
                        oldest_index = i;
                    }
                }
            }

            if (oldest_index < 0)
                return false;

            var target_list = dict[oldest_pawn_id];
            target_list.RemoveAt(oldest_index);
            total_count--;
            if (target_list.Count == 0)
            {
                dict.Remove(oldest_pawn_id);
            }
            return true;
        }
    }
}
