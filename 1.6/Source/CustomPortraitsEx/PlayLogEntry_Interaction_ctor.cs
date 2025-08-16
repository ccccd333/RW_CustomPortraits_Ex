using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RimWorld.ColonistBar;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{
    [HarmonyPatch]
    public static class PlayLogEntry_Interaction_ctor
    {
        private static Dictionary<string, PawnCacheEntry> RecipientLogDict = new Dictionary<string, PawnCacheEntry>();
        private static Queue<string> RecipientLogOrder = new Queue<string>();

        private static Dictionary<string, PawnCacheEntry> InitiatorLogDict = new Dictionary<string, PawnCacheEntry>();
        private static Queue<string> InitiatorLogOrder = new Queue<string>();

        public static ConstructorInfo TargetMethod()
        {
            return AccessTools.Constructor(
                typeof(PlayLogEntry_Interaction),
                new System.Type[] {
                typeof(InteractionDef),
                typeof(Pawn),
                typeof(Pawn),
                typeof(List<RulePackDef>)
                }
            );
        }

        public static bool Prefix(InteractionDef intDef, Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks)
        {
            // PlayLog.Add()の引数に使われるPlayLogEntry_Interactionのctor
            try
            {
                // 別スレッドかどうか確認しておく ver1.6以降→要確認
                // もしスレッドIDが違う場合はlock
                //Log.Message($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                if (!PortraitCacheEx.IsAvailable) return true;

                Log.Message($"[PortraitsEx] InteractionDef {intDef.LabelCap} initiator {initiator.Name.ToStringFull} recipient {recipient.Name.ToStringFull}");
                CleanupExpiredAndExcessLogs();

                var ismap = PortraitCacheEx.InteractionSelectionMap;
                foreach (var intef in ismap.InteractionFilter)
                {
                    var filter = intef.Value;
                    if (!filter.is_recipient && !filter.is_initiator) continue;
                    //Log.Message($"[PortraitsEx] intef.Key {intef.Key}");
                    if (ismap.intf_regex_cache.ContainsKey(intef.Key))
                    {
                        var reg = ismap.intf_regex_cache[intef.Key];
                        if (reg.IsMatch(intDef.LabelCap))
                        {
                            //Log.Message($"[PortraitsEx] InteractionDef {intDef.LabelCap} intef.Key {intef.Key}");
                            PushDict(intDef.LabelCap, filter, initiator, recipient);
                        }
                    }
                    else
                    {
                        if (intDef.LabelCap == intef.Key)
                        {
                            //Log.Message($"[PortraitsEx] InteractionDef {intDef.LabelCap} intef.Key {intef.Key}");
                            PushDict(intDef.LabelCap, filter, initiator, recipient);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message($"[PortraitsEx] Please notify me if this error log is generated. wt ==> {e.Message}");
                //Log.Warning($"[PortraitsEx] An unrecognized error occurred. intDef.LabelCap: {intDef.LabelCap} initiator: {initiator.Name.ToStringFull} recipient {recipient.Name.ToStringFull} ===> wt?: {e.Message}");
            }

            return true;
        }

        public static void CleanupExpiredAndExcessLogs()
        {
            if (InitiatorLogDict.Count > 0)
            {
                CleanupExpiredAndExcessLogsImpl(InitiatorLogDict, InitiatorLogOrder, PortraitCacheEx.Settings.initiator_log_retention.max_entries);
            }
            if (RecipientLogDict.Count > 0)
            {
                CleanupExpiredAndExcessLogsImpl(RecipientLogDict, RecipientLogOrder, PortraitCacheEx.Settings.recipient_log_retention.max_entries);
            }
        }

        public static List<string> GetAllKeysByPawnTrimmedFinal(Pawn targetPawn)
        {
            //foreach (var kvp in RecipientLogDict)
            //{
            //    Log.Message($"[PortraitsEx] {kvp.Key} matched_key: {kvp.Value.matched_key} {kvp.Value.pawn}");
            //}

            //foreach (var kvp in InitiatorLogDict)
            //{
            //    Log.Message($"[PortraitsEx] {kvp.Key} matched_key: {kvp.Value.matched_key} {kvp.Value.pawn}");
            //}

            var recipient_keys = RecipientLogDict
                .Where(kvp => kvp.Value.pawn == targetPawn)
                .Select(kvp =>
                {
                    if (kvp.Value.matched_key != "") return kvp.Value.matched_key;
                    int atIndex = kvp.Key.IndexOf('@');
                    return atIndex >= 0 ? kvp.Key.Substring(0, atIndex) : kvp.Key;
                });

            var initiator_keys = InitiatorLogDict
                .Where(kvp => kvp.Value.pawn == targetPawn)
                .Select(kvp =>
                {
                    if (kvp.Value.matched_key != "") return kvp.Value.matched_key;
                    int atIndex = kvp.Key.IndexOf('@');
                    return atIndex >= 0 ? kvp.Key.Substring(0, atIndex) : kvp.Key;
                });

            return recipient_keys
                .Concat(initiator_keys)
                .Distinct()
                .ToList();
        }

        private static void PushDict(string label_cap, InteractionFilter filter, Pawn initiator, Pawn recipient)
        {
            //Log.Message($"[PortraitsEx] PushDict START");
            float cache_duration_seconds = filter.cache_duration_seconds;
            //Log.Message($"[PortraitsEx] PushDict label_cap: {label_cap} is_initiator: {filter.is_initiator} is_recipient: {filter.is_recipient} matched_initiator_key: {filter.matched_initiator_key} matched_recipient_key: {filter.matched_recipient_key} cache_duration_seconds: {filter.cache_duration_seconds}");
            if (cache_duration_seconds <= 0.0001f) return;
            //int a = PortraitCacheEx.Settings.initiator_log_retention.max_entries;
            //Log.Message($"[PortraitsEx] PushDict SEP1");
            if (filter.is_initiator)
            {
                //Log.Message($"[PortraitsEx] PushDict SEP2");

                if (initiator != null &&initiator.Name != null)
                {
                    AddOrUpdate(InitiatorLogDict, InitiatorLogOrder, $"{label_cap}@{initiator.Name.ToStringFull}", filter.matched_initiator_key, initiator, cache_duration_seconds, PortraitCacheEx.Settings.initiator_log_retention.max_entries);
                }
                //Log.Message($"[PortraitsEx] PushDict SEP3");
            }
            //Log.Message($"[PortraitsEx] PushDict SEP4");
            if (filter.is_recipient)
            {
               //Log.Message($"[PortraitsEx] PushDict SEP5");
                if (recipient != null && recipient.Name != null)
                {
                    AddOrUpdate(RecipientLogDict, RecipientLogOrder, $"{label_cap}@{recipient.Name.ToStringFull}", filter.matched_recipient_key, recipient, cache_duration_seconds, PortraitCacheEx.Settings.recipient_log_retention.max_entries);
                }
                //Log.Message($"[PortraitsEx] PushDict SEP6");
            }
            //Log.Message($"[PortraitsEx] PushDict END");
        }

        private static void AddOrUpdate(Dictionary<string, PawnCacheEntry> dict, Queue<string> order, string key, string matched_key, Pawn value, float cache_duration_seconds, int max_entries)
        {
            //Log.Message($"[PortraitsEx] AddOrUpdate START que: {key} matched_key: {matched_key} pawn: {value}");
            if (!dict.ContainsKey(key))
            {
                //Log.Message($"[PortraitsEx] AddOrUpdate SEP1 que: {key} matched_key: {matched_key} pawn: {value}");
                if (dict.Count >= max_entries)
                {
                    string oldestKey = order.Dequeue();
                    //Log.Message($"[PortraitsEx] Dequeue oldestKey: {oldestKey} que: {key} matched_key: {matched_key} pawn: {value}");
                    dict.Remove(oldestKey);
                    //Log.Message($"[PortraitsEx] dict count {dict.Count}");
                }
                //Log.Message($"[PortraitsEx] AddOrUpdate SEP2 que: {key} matched_key: {matched_key} pawn: {value}");
                order.Enqueue(key);
            }
            //Log.Message($"[PortraitsEx] AddOrUpdate SEP3 que: {key} matched_key: {matched_key} pawn: {value}");
            dict[key] = new PawnCacheEntry(value, matched_key, cache_duration_seconds);

            //Log.Message($"[PortraitsEx] AddOrUpdate END que: {key} matched_key: {matched_key} pawn: {value}");
        }

        private static void CleanupExpiredAndExcessLogsImpl(Dictionary<string, PawnCacheEntry> dict, Queue<string> order, int max_entries)
        {
            var now = Time.realtimeSinceStartup;
            //Log.Message($"[PortraitsEx] CleanupExpiredAndExcessLogs Remove START");

            while (order.Count > 0)
            {
                var oldestKey = order.Peek();
                bool isExpired = dict.TryGetValue(oldestKey, out var entry) && (now - entry.cached_at >= entry.cache_duration_seconds);
                bool isOverMax = dict.Count > max_entries;

                if (isExpired || isOverMax)
                {
                    //Log.Message($"[PortraitsEx] CleanupExpiredAndExcessLogs Remove ==> oldestKey: {oldestKey} now: {now} cached_at: {entry.cached_at} cache_duration_seconds:{entry.cache_duration_seconds}");
                    order.Dequeue();
                    dict.Remove(oldestKey);
                }
                else
                {
                    break;
                }
            }

            //Log.Message($"[PortraitsEx] CleanupExpiredAndExcessLogs Remove END");
        }
    }
}