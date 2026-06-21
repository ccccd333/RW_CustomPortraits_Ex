
using CustomPortraits;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx
{

    public static class ConditionDrivenPortrait
    {
        private static List<Texture2D> temp = new List<Texture2D>();
        private static int temp_index = 0;
        private static string temp_refs_key = "";
        private static bool temp_animation_mode = false;
        private static string temp_preset_name = "";
        private static float temp_display_duration = 2.0f;

        private static float last_update_time = Time.realtimeSinceStartup;
        private static float frame_interval_seconds = 0.1f;
        private static bool portrait_skip_on_lag = true;

        private static float disp_last_update_time = Time.realtimeSinceStartup;

        private static bool is_interrupt_active = false;

        private class AsyncRequest
        {
            public string preset_name;
            public Refs refs;
            public bool is_interrupt_active;
            public bool intr_is_value_fetched;
            public Dictionary<string, float> intr_impact_map;
            public bool steady_is_value_fetched;
            public Dictionary<string, float> steady_impact_map;
        }

        private static System.Collections.Concurrent.ConcurrentQueue<AsyncRequest> requestQueue = new System.Collections.Concurrent.ConcurrentQueue<AsyncRequest>();
        private static AutoResetEvent thread_event = new AutoResetEvent(false);
        private static Thread worker_thread;
        private static volatile string pending_context_result = null;
        private static volatile bool is_calculating = false;
        private static volatile string current_calculating_preset = null;

        static ConditionDrivenPortrait()
        {
            worker_thread = new Thread(WorkerLoop);
            worker_thread.IsBackground = true;
            worker_thread.Name = "CustomPortraitsAsyncWorker";
            worker_thread.Start();
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                thread_event.WaitOne();
                while (requestQueue.TryDequeue(out AsyncRequest req))
                {
                    if (req.preset_name != current_calculating_preset) continue;

                    try
                    {
                        string portrait_context_name = "";
                        bool is_resolved = false;
                        bool no_match = false;

                        if (req.is_interrupt_active && req.intr_is_value_fetched)
                        {
                            portrait_context_name = ResolveInterruptPortraitContextName(null, req.refs, req.intr_impact_map, out is_resolved, out no_match);
                            if (!is_resolved)
                            {
                                portrait_context_name = "def";
                            }
                            else if (no_match && PortraitCacheEx.Settings.interrupt_fallback_to_steady)
                            {
                                if (req.steady_is_value_fetched)
                                {
                                    portrait_context_name = ResolveSteadyPortraitContextName(null, req.refs, req.steady_impact_map, out is_resolved);
                                    if (!is_resolved) portrait_context_name = "def";
                                }
                                else
                                {
                                    portrait_context_name = "def";
                                }
                            }
                        }
                        else
                        {
                            if (req.steady_is_value_fetched)
                            {
                                portrait_context_name = ResolveSteadyPortraitContextName(null, req.refs, req.steady_impact_map, out is_resolved);
                                if (!is_resolved) portrait_context_name = "def";
                            }
                            else
                            {
                                portrait_context_name = "def";
                            }
                        }

                        pending_context_result = portrait_context_name;
                    }
                    catch (Exception ex)
                    {
                        // [DEBUG] エラー時ログ
                        // System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "thread_debug.log"), $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Exception: {ex}\n");
                        
                        Log.Error($"[PortraitsEx] Async Worker Exception: {ex}");
                        pending_context_result = "def";
                    }
                    finally
                    {
                        is_calculating = false;
                    }
                }

                // [DEBUG] キューの処理をすべて終えてスリープ（待機状態）に入る直前のログ
                // System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "thread_debug.log"), $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Thread Finished Processing Queue and goes to sleep.\n");
            }
        }

        public static void Reset()
        {
            // ゲームロード開始時などに入ってくる
            while (requestQueue.TryDequeue(out _)) { }
            is_calculating = false;
            current_calculating_preset = null;
            pending_context_result = null;

            //temp.Clear();
            temp_index = 0;
            temp_refs_key = "";
            temp_animation_mode = false;
            temp_preset_name = "";
            temp_display_duration = PortraitCacheEx.Settings.display_duration;
            last_update_time = Time.realtimeSinceStartup;
            disp_last_update_time = Time.realtimeSinceStartup;
            is_interrupt_active = false;

            // settings反映
            portrait_skip_on_lag = PortraitCacheEx.Settings.portrait_animation.portrait_skip_on_lag;
            frame_interval_seconds = PortraitCacheEx.Settings.portrait_animation.frame_interval_seconds;
        }



        public static Texture2D GetPortraitTexture(Pawn pawn, string filename, Texture2D def)
        {
            //Log.Message($"[PortraitsEx] Try Visible Portrait: test 1");
            if (filename != null && filename != "" && PortraitCacheEx.IsAvailable)
            {
                string preset_name = "";
                string d = "";
                preset_name = Utility.Delimiter(filename, out d);
                //Log.Message($"[PortraitsEx] Try Visible Portrait: test 2");
                if (preset_name == "")
                {
                    // たぶんないとは思うけど。
                    return def;
                }
                //Log.Message($"[PortraitsEx] Try Visible Portrait: test 3");
                bool next_portrait = false;
                int skip_count = 1;
                // ゲーム内時間だとFPSに依存してしまうのでUnityの内部タイマーでフレーム計算する
                float current_time = Time.realtimeSinceStartup;
                //Log.Message($"[PortraitsEx] Try Visible Portrait: test 4");
                if (current_time - last_update_time >= frame_interval_seconds)
                {
                    // TODO:ここはアニメーション機能前提に組んでいるのでちょっと変だけど。その内直すかも

                    // 大体0.1sだと60FPSで4か6フレーム目くらいで次の画像表示する
                    next_portrait = true;

                    if (portrait_skip_on_lag)
                    {
                        // コロニー終盤だとFPSが低下するので、それ用に表示画像のスキップ機能を追加

                        // 現在の時刻と前フレームの時刻を計算して、frame_interval_secondsに
                        // 収まらない場合はその分スキップする。
                        float delta = current_time - last_update_time;
                        skip_count = Mathf.FloorToInt(delta / frame_interval_seconds);
                        // ここでcurrent_timeを入れると余り分が消失してしまう
                        // スキップタイミングはframe_interval_secondsの1倍の時は1枚画像送りでいいが
                        // 2倍の場合は0.21や0.22と0.0Xとなる。この余りも次インターバルに含めるため。
                        last_update_time += skip_count * frame_interval_seconds;
                        //if (skip_count > 1)
                        //{
                        //    Log.Message($"[PortraitsEx] Skip Cound {skip_count} current_time {current_time} last_update_time {last_update_time}");
                        //}
                    }
                    else
                    {
                        last_update_time = current_time;

                    }
                }
                var mood_refs = PortraitCacheEx.Refs;
                //Log.Message($"[PortraitsEx] Try Visible Portrait: test 5");
                if (mood_refs.ContainsKey(preset_name) && !PortraitCacheEx.PresetErrorMap.ContainsKey(preset_name))
                {
                    //Log.Message($"[PortraitsEx] Try Visible Portrait: test 6");

                    if (temp_preset_name != preset_name)
                    {
                        bool isHandlingAsync = (is_calculating || pending_context_result != null) && current_calculating_preset == preset_name;
                        if (!isHandlingAsync)
                        {
                            //Log.Message($"[PortraitsEx] Try Visible Portrait: test 7 isHandlingAsync: {isHandlingAsync} isCalculating: {isCalculating} currentCalculatingPreset: {currentCalculatingPreset} temp_preset_name: {temp_preset_name} preset_name: {preset_name}");
                            // ポートレートが別々のポーンの場合、退避情報をクリアして、後続処理をする。
                            Reset();
                        }
                    }

                    var refs = mood_refs[preset_name];
                    string portrait_context_name = "";

                    if (pawn.Dead)
                    {
                        // ポーンが死亡している場合
                        if (refs.fallback_mood_on_death == "") return def;
                        // 死んでいる場合はjsonのfallback_mood_on_deathを使う。
                        portrait_context_name = refs.fallback_mood_on_death;
                    }
                    else
                    {
                        //if (Settings.Instance.debug)
                        //{
                        //    if (pendingContextResult != null)
                        //        Log.Message($"[PortraitsEx] ConditionDrivenPortrait.GetPortraitTexture {pendingContextResult}");
                        //}

                        bool intr_is_value_fetched = false;
                        Dictionary<string, float> intr_impact_map = new Dictionary<string, float>();
                        if (refs.interrupt.interrupt_enabled)
                        {
                            intr_impact_map = PawnPortraitInterruptContext.ComposeImpactMap(pawn, refs.interrupt, is_interrupt_active, out intr_is_value_fetched);
                        }

                        bool isHandlingAsync = (is_calculating || pending_context_result != null) && current_calculating_preset == preset_name;
                        if (isHandlingAsync)
                        {
                            if (pending_context_result != null)
                            {
                                portrait_context_name = pending_context_result;
                                pending_context_result = null;
                                disp_last_update_time = Time.realtimeSinceStartup;
                            }
                            else
                            {
                                return next_portrait ? AdvanceToNextPortrait(def, skip_count) : GetCurrentPortrait(def);
                            }
                        }
                        else
                        {
                            if (!is_interrupt_active && intr_is_value_fetched)
                            {
                                is_interrupt_active = true;
                            }
                            else
                            {
                                if (temp_preset_name == preset_name)
                                {
                                    if (current_time - disp_last_update_time <= temp_display_duration)
                                    {
                                        //if (Settings.Instance.debug)
                                        //{
                                        //    Log.Message($"[PortraitsEx] ConditionDrivenPortrait.GetPortraitTexture portrait_context_name: {portrait_context_name} current_time: {current_time} disp_last_update_time: {disp_last_update_time} temp_display_duration: {temp_display_duration}");
                                        //}

                                        return next_portrait ? AdvanceToNextPortrait(def, skip_count) : GetCurrentPortrait(def);
                                    }
                                }

                                if (is_interrupt_active)
                                {
                                    portrait_context_name = temp_refs_key;
                                    is_interrupt_active = false;
                                    disp_last_update_time = Time.realtimeSinceStartup;
                                }
                            }

                            if (portrait_context_name == "")
                            {
                                bool steady_is_value_fetched = false;
                                Dictionary<string, float> steady_impact_map = new Dictionary<string, float>();

                                if ((is_interrupt_active && PortraitCacheEx.Settings.interrupt_fallback_to_steady) || !is_interrupt_active)
                                {
                                    steady_impact_map = PawnPortraitContext.ComposeImpactMap(pawn, out steady_is_value_fetched);
                                }

                                is_calculating = true;
                                current_calculating_preset = preset_name;
                                pending_context_result = null;

                                requestQueue.Enqueue(new AsyncRequest
                                {
                                    preset_name = preset_name,
                                    refs = refs,
                                    is_interrupt_active = is_interrupt_active,
                                    intr_is_value_fetched = intr_is_value_fetched,
                                    intr_impact_map = intr_impact_map,
                                    steady_is_value_fetched = steady_is_value_fetched,
                                    steady_impact_map = steady_impact_map
                                });
                                thread_event.Set();

                                return next_portrait ? AdvanceToNextPortrait(def, skip_count) : GetCurrentPortrait(def);
                            }
                        }
                    }
                    return UpdatePortraitByContext(portrait_context_name, next_portrait, skip_count, def, refs, preset_name);


                }
            }

            return def;
        }

        private static string ResolveSteadyPortraitContextName(Pawn pawn, Refs refs, Dictionary<string, float> impact_map, out bool is_resolved)
        {
            string portrait_context_name = "";
            //Dictionary<string, float> impact_map;

            is_resolved = false;

            //foreach (var k in refs.group_filter)
            //{
            //    Log.Message($"[PortraitsEx] refs.group_filter ==> Key {k.Key} Value {k.Value.key}");
            //}

            //foreach (var k in refs.g_regex_cache)
            //{
            //    Log.Message($"[PortraitsEx] refs.g_regex_cache ==> Key {k.Key} Value {k.Value}");
            //}

            //foreach (var k in impact_map)
            //{
            //    Log.Message($"[PortraitsEx] impact_map ==> Key {k.Key} Value {k.Value}");
            //}


            // jsonのグループのキー(Group名)と値(心情+社交名)mood_and_last_social(心情+社交の文字と値)のキーと一致する場合
            // filtered_group_filterに一旦重複してもいいので入れていく。
            var filtered_group_filter = FilterGroupMatches(refs.g_regex_cache, refs.group_filter, impact_map);

            if (filtered_group_filter.Count() <= 0 && impact_map.Count() <= 0)
            {
                return "";
            }

            //foreach (var test in filtered_group_filter)
            //{
            //    Log.Message($"[PortraitsEx] filtered_group_filter: {test.Key} {test.Value}");
            //}

            // jsonのpriority_weightsの上から順にとmerged_keysのキーと突き合わせて行く。
            // priority_weightsと一致するもののみがmatched_priority_weightsに入る。
            var matched_priority_weights = ExtractMatchedPriorityWeights(refs.priority_weight_order, refs.priority_weights, filtered_group_filter);

            //int logc = 1;
            //foreach (var mpw in matched_priority_weights)
            //{
            //    foreach (var kvp in mpw)
            //    {
            //        Log.Message($"[PortraitsEx] Matched Priority Weights priority: {logc} category ==> {kvp.Value.category} mood ==> {kvp.Value.filter_name} weight: {kvp.Value.weight}");
            //        ++logc;
            //    }
            //}


            // matched_priority_weightsの始まりから順に優先となっているので、
            // weightとランダム結果を比べて、weight以下だったらその名前を後続へ。
            foreach (var elm in matched_priority_weights)
            {
                bool pic = false;
                foreach (var kvp in elm)
                {
                    int weight = kvp.Value.weight;
                    int seed = UnityEngine.Random.Range(0, 100);
                    //Log.Message($"[PortraitsEx] name: {kvp.Value.filter_name} seed: {seed} weight: {weight}");
                    if (seed < weight)
                    {
                        portrait_context_name = kvp.Value.filter_name;
                        pic = true;
                        break;
                    }
                }

                if (pic)
                {
                    break;
                }
            }


            //Log.Message($"[PortraitsEx] portrait_context_name: {portrait_context_name} ");

            // 抽出した心情+社交値名がなければ、jsonのfallback_moodを使う。
            if (portrait_context_name == "")
            {
                if (refs.fallback_mood == "")
                {
                    portrait_context_name = "def";
                }
                else
                {
                    portrait_context_name = refs.fallback_mood;
                }
            }
            is_resolved = true;
            return portrait_context_name;
        }

        private static string ResolveInterruptPortraitContextName(Pawn pawn, Refs refs, Dictionary<string, float> impact_map, out bool is_resolved, out bool no_match)
        {
            string portrait_context_name = "";
            //Dictionary<string, float> impact_map;

            is_resolved = false;
            no_match = false;

            //foreach (var k in refs.interrupt.group_filter)
            //{
            //    Log.Message($"[PortraitsEx] refs.interrupt.group_filter ==> Key {k.Key} Value {k.Value.key}");
            //}

            //foreach (var k in impact_map)
            //{
            //    Log.Message($"[PortraitsEx] impact_map ==> Key {k.Key} Value {k.Value}");
            //}


            // jsonのグループのキー(Group名)と値(心情+社交名)mood_and_last_social(心情+社交の文字と値)のキーと一致する場合
            // filtered_group_filterに一旦重複してもいいので入れていく。
            var filtered_group_filter = FilterGroupMatches(null, refs.interrupt.group_filter, impact_map);

            if (filtered_group_filter.Count() <= 0 && impact_map.Count() <= 0)
            {
                return "";
            }

            //foreach (var test in filtered_group_filter)
            //{
            //    Log.Message($"[PortraitsEx] Interrupt filtered_group_filter: {test.Key} {test.Value}");
            //}

            // jsonのpriority_weightsの上から順にとmerged_keysのキーと突き合わせて行く。
            // priority_weightsと一致するもののみがmatched_priority_weightsに入る。
            var matched_priority_weights = ExtractMatchedPriorityWeights(refs.interrupt.priority_weight_order, refs.interrupt.priority_weights, filtered_group_filter);

            //int logc = 1;
            //foreach (var mpw in matched_priority_weights)
            //{
            //    foreach (var kvp in mpw)
            //    {
            //        Log.Message($"[PortraitsEx] Matched Priority Weights priority: {logc} category ==> {kvp.Value.category} mood ==> {kvp.Value.filter_name} weight: {kvp.Value.weight}");
            //        ++logc;
            //    }
            //}


            // matched_priority_weightsの始まりから順に優先となっているので、
            // weightとランダム結果を比べて、weight以下だったらその名前を後続へ。
            foreach (var elm in matched_priority_weights)
            {
                bool pic = false;
                foreach (var kvp in elm)
                {
                    int weight = kvp.Value.weight;
                    int seed = UnityEngine.Random.Range(0, 100);
                    //Log.Message($"[PortraitsEx] name: {kvp.Value.filter_name} seed: {seed} weight: {weight}");
                    if (seed < weight)
                    {
                        portrait_context_name = kvp.Value.filter_name;
                        pic = true;
                        break;
                    }
                }

                if (pic)
                {
                    break;
                }
            }


            //Log.Message($"[PortraitsEx] portrait_context_name: {portrait_context_name} ");

            // 全てマッチしない場合、jsonのfallback_moodを使う。
            if (portrait_context_name == "")
            {
                no_match = true;

                if (refs.fallback_mood == "")
                {
                    portrait_context_name = "def";
                }
                else
                {
                    portrait_context_name = refs.fallback_mood;
                }
            }
            is_resolved = true;
            return portrait_context_name;
        }


        private static Texture2D UpdatePortraitByContext(string portrait_context_name, bool next_portrait, int skip_count, Texture2D def, Refs refs, string preset_name)
        {
            if (portrait_context_name != temp_refs_key)
            {
                // 心情+社交名と既に退避済みの心情+社交名が一致しないとき
                Reset();
                string access_key = "";

                if (refs.MatchDictKeysByRegex(portrait_context_name, out access_key))
                {
                    return CacheTextureIfKeyMatches(def, refs, preset_name, access_key);

                    //var txs = refs.txs;
                    //var tt = txs[access_key];
                    //temp = tt.txs;
                    //temp_animation_mode = tt.IsAnimation;
                    //temp_index = 0;
                    //temp_preset_name = preset_name;
                    //temp_refs_key = access_key;
                    //temp_display_duration = tt.display_duration;
                    ////Log.Message($"[PortraitsEx] preset_name {temp_preset_name} disp_d {temp_display_duration}");
                    //if (temp.Count <= 0)
                    //{
                    //    return ImageLoadError(preset_name, def);
                    //}
                    //else
                    //{
                    //    if (temp.Count <= 0) return ImageLoadError(preset_name, def);

                    //    Texture2D texture = temp[temp_index];
                    //    if (temp_animation_mode) ++temp_index;

                    //    if (texture == null)
                    //    {
                    //        Log.Error("[PortraitsEx] The image was successfully generated, but disappeared at the point when it was registered in the dictionary.");
                    //        return ImageLoadError(preset_name, def);
                    //    }
                    //    return texture;
                    //}
                }
                else
                {
                    if (refs.fallback_mood != "" && refs.MatchDictKeysByRegex(refs.fallback_mood, out access_key))
                    {
                        return CacheTextureIfKeyMatches(def, refs, preset_name, access_key);
                    }
                    else
                    {
                        return ImageLoadError(preset_name, def);
                    }
                }
            }
            else
            {
                if (next_portrait)
                {

                    return AdvanceToNextPortrait(def, skip_count);
                }
                else
                {

                    return GetCurrentPortrait(def);
                }
            }
        }

        private static Texture2D CacheTextureIfKeyMatches(Texture2D def, Refs refs, string preset_name, string access_key)
        {
            // 心情+社交値名とjsonのmood_refsのキー名と一致するものがあるとき
            // 次回から同じ重い処理しないようにするため、画像表示用の変数を退避する。
            var txs = refs.txs;
            var tt = txs[access_key];
            temp = tt.txs;
            temp_animation_mode = tt.IsAnimation;
            temp_index = 0;
            temp_preset_name = preset_name;
            temp_refs_key = access_key;
            temp_display_duration = tt.display_duration;

            //Log.Message($"[PortraitsEx] preset_name {temp_preset_name} disp_d {temp_display_duration}");
            if (temp.Count <= 0)
            {
                return ImageLoadError(preset_name, def);
            }
            else
            {
                if (temp.Count <= 0) return ImageLoadError(preset_name, def);

                Texture2D texture = temp[temp_index];
                if (temp_animation_mode) ++temp_index;

                if (texture == null)
                {
                    Log.Error("[PortraitsEx] The image was successfully generated, but disappeared at the point when it was registered in the dictionary.");
                    return ImageLoadError(preset_name, def);
                }
                return texture;
            }
        }

        private static Texture2D AdvanceToNextPortrait(Texture2D def, int skip_count)
        {
            if (temp.Count <= 0) return def;
            if (temp_index < temp.Count)
            {
                Texture2D tx = temp[temp_index];
                if (temp_animation_mode) temp_index += skip_count;
                return tx;
            }
            else
            {
                temp_index = 0;
                return temp[temp_index];
            }
        }

        private static Texture2D GetCurrentPortrait(Texture2D def)
        {
            if (temp.Count <= 0) return def;
            if (temp_animation_mode)
            {
                if (temp_index < temp.Count)
                {
                    return temp[temp_index];
                }
                else
                {
                    return temp[0];
                }
            }
            else
            {
                return temp[0];
            }
        }

        private static Dictionary<string, string> FilterGroupMatches(
            Dictionary<string, IPatternMatcher> g_regex_cache,
            Dictionary<string, GroupPatternEntry> group_filter,
            Dictionary<string, float> impact_map)
        {
            Dictionary<string, string> filtered_group_filter = new Dictionary<string, string>();
            foreach (var kvp in group_filter)
            {
                bool is_matched = false;
                if (filtered_group_filter.ContainsKey(kvp.Value.key))
                {
                    // Groupは値:キーになってるので、同じキーがあったら後続の処理で上書きされるだけなので
                    // 一致確認する必要なしなのでスキップ
                    continue;
                }
                else if (g_regex_cache != null && g_regex_cache.TryGetValue(kvp.Key, out var matcher))
                {
                    foreach (var impact_map_key in impact_map.Keys)
                    {
                        if (matcher.IsMatch(impact_map_key))
                        {
                            filtered_group_filter[kvp.Value.key] = kvp.Value.key;
                            is_matched = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (impact_map.ContainsKey(kvp.Key)) 
                    {
                        filtered_group_filter[kvp.Value.key] = kvp.Value.key;
                        is_matched = true;
                    }
                }

                if (is_matched)
                {
                    if (kvp.Value.alias_targets.Count > 0)
                    {
                        // alias_targetsにimpact_map_keyとマッチするものがあれば、そちらも追加する
                        foreach (var alias in kvp.Value.alias_targets)
                        {
                            //Log.Message($"[PortraitsEx] alias_targets Key: {kvp.Value.key} Alias: {alias}");
                            filtered_group_filter[alias] = alias;
                        }
                    }
                }
            }

            return filtered_group_filter;
        }

        private static List<Dictionary<string, PriorityWeights>> ExtractMatchedPriorityWeights(
            List<string> priority_weight_order,
            //Dictionary<string, Regex> pw_regex_cache,
            Dictionary<string, PriorityWeights> priority_weights,
            Dictionary<string, string> merged_keys)
        {
            var matched_priority_weights = new List<Dictionary<string, PriorityWeights>>();
            foreach (var kp in priority_weight_order)
            {
                //bool match_found = false;

                var vp = priority_weights[kp];
                Dictionary<string, PriorityWeights> dict = new Dictionary<string, PriorityWeights>();
                //if (pw_regex_cache != null && pw_regex_cache.ContainsKey(kp))
                //{
                //    var reg = pw_regex_cache[kp];
                //    var lis = new List<string>();

                //    foreach (var mk in merged_keys)
                //    {
                //        if (reg.IsMatch(mk.Key))
                //        {
                //            lis.Add(mk.Key);
                //            match_found = true;
                //            break;
                //        }
                //    }

                //    if (match_found)
                //    {
                //        foreach (var elm in lis)
                //        {
                //            if (!dict.ContainsKey(elm))
                //            {
                //                dict.Add(elm, vp);
                //            }
                //        }
                //    }
                //}
                //else
                {
                    if (merged_keys.ContainsKey(kp))
                    {
                        if (!dict.ContainsKey(kp))
                        {
                            dict.Add(kp, vp);
                        }
                    }
                        
                    //    match_found = true;

                    //if (match_found)
                    //{
                    //    if (!dict.ContainsKey(kp))
                    //    {
                    //        dict.Add(kp, vp);
                    //    }
                    //}
                }

                matched_priority_weights.Add(dict);
            }

            return matched_priority_weights;
        }


        private static Texture2D ImageLoadError(string preset_name, Texture2D def)
        {
            temp = new List<Texture2D>() { def };
            temp_animation_mode = false;
            temp_index = 0;
            temp_preset_name = preset_name;
            temp_refs_key = "def";
            temp_display_duration = PortraitCacheEx.Settings.display_duration;

            return def;
        }
    }
}
