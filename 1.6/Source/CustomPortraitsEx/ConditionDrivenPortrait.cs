
using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
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
        private static float seconds_interval = 0.1f;

        private static float disp_last_update_time = Time.realtimeSinceStartup;

        public static void Reset()
        {
            //temp.Clear();
            temp_index = 0;
            temp_refs_key = "";
            temp_animation_mode = false;
            temp_preset_name = "";
            temp_display_duration = PortraitCacheEx.Settings.display_duration;
            last_update_time = Time.realtimeSinceStartup;
            disp_last_update_time = Time.realtimeSinceStartup;
        }



        public static Texture2D GetPortraitTexture(Pawn pawn, string filename, Texture2D def)
        {
            //Log.Message($"[PortraitsEx] Try Visible Portrait: {filename}");
            if (filename != null && filename != "" && PortraitCacheEx.IsAvailable)
            {
                string preset_name = "";
                string d = "";
                preset_name = Utility.Delimiter(filename, out d);

                if (preset_name == "")
                {
                    // たぶんないとは思うけど。
                    return def;
                }

                bool nextPortrait = false;
                // ゲーム内時間だとFPSに依存してしまうのでUnityの内部タイマーでフレーム計算する
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - last_update_time >= seconds_interval)
                {
                    // 大体60FPSで4フレーム目くらいで次の画像表示する
                    nextPortrait = true;
                    last_update_time = currentTime;
                }
                var mood_refs = PortraitCacheEx.Refs;
                if (mood_refs.ContainsKey(preset_name))
                {

                    if (temp_preset_name != preset_name)
                    {
                        //Log.Message($"[PortraitsEx] temp_preset_name {temp_preset_name} preset_name {preset_name}");

                        // ポートレートが別々のポーンの場合、退避情報をクリアして、後続処理をする。
                        Reset();
                    }
                    else
                    {
                        //Log.Message($"[PortraitsEx] disp_last_update_time ==> {disp_last_update_time} currentTime ==> {currentTime} temp_display_duration ==> {temp_display_duration}");

                        // 毎回後続の重い処理を実行したくないのでjsonのdisplay_durationの間は退避した情報で
                        // アニメーションor画像表示を行う。
                        if (currentTime - disp_last_update_time <= temp_display_duration)
                        {
                            if (nextPortrait)
                            {
                                return AdvanceToNextPortrait(def);
                            }
                            else
                            {
                                return GetCurrentPortrait(def);
                            }
                        }
                        else
                        {
                            disp_last_update_time = Time.realtimeSinceStartup;
                        }
                    }

                    var refs = mood_refs[preset_name];


                    string mood_name = "";

                    if (pawn.Dead)
                    {
                        // ポーンが死亡している場合thoughtsがnullを返すため、
                        if (refs.fallback_mood_on_death == "") return def;
                        // 死んでいる場合はjsonのfallback_mood_on_deathを使う。
                        mood_name = refs.fallback_mood_on_death;
                    }
                    else
                    {
                        Dictionary<string, float> affection_impact_map;
                        bool is_value_fetched = false;
                        affection_impact_map = BuildAffectionImpactMap(pawn, out is_value_fetched);

                        if (!is_value_fetched)
                        {
                            Reset();
                            return def;
                        }
                        //foreach(var k in refs.group_filter)
                        //{
                        //    Log.Message($"[PortraitsEx] refs.group_filter ==> Key {k.Key} Value {k.Value}");
                        //}

                        //foreach (var k in mood)
                        //{
                        //    Log.Message($"[PortraitsEx] mood ==> Key {k.Key} Value {k.Value}");
                        //}


                        // jsonのグループのキー(Group名)と値(心情+社交名)mood_and_last_social(心情+社交の文字と値)のキーと一致する場合
                        // filtered_group_filterに一旦重複してもいいので入れていく。
                        var filtered_group_filter = FilterGroupMatches(refs, affection_impact_map);

                        // 心情+社交が一切ない(?)。
                        if (filtered_group_filter.Count() <= 0 && affection_impact_map.Count() <= 0) return def;


                        var matched_group = new Dictionary<string, List<string>>();

                        // filtered_group_filterをキー：値のものを、値：キーにしていく。
                        // 同時に重複した値を取り除いていく
                        foreach (var kvp in filtered_group_filter)
                        {
                            if (!matched_group.ContainsKey(kvp.Value))
                            {
                                matched_group[kvp.Value] = new List<string>();
                            }
                            matched_group[kvp.Value].Add(kvp.Key);

                            //Log.Message($"[PortraitsEx] aaaa mood: {kvp.Value} {kvp.Key}");
                        }

                        var merged_keys = new Dictionary<string, string>();
                        // mood側を先に入れる（値はnull）
                        foreach (var kvp in affection_impact_map)
                        {
                            //Log.Message($"[PortraitsEx] pic mood: {kvp.Key}");
                            merged_keys[kvp.Key] = null;
                        }

                        // group側を上書き（値を反映）
                        // これでmerged_keys=mood＋group(グループ名)という形になる。
                        foreach (var kvp in matched_group)
                        {
                            //Log.Message($"[PortraitsEx] pic group: {kvp.Key} relative mood ==> {kvp.Value}");
                            merged_keys[kvp.Key] = kvp.Key;
                        }

                        //foreach (var test in merged_keys)
                        //{
                        //    Log.Message($"[PortraitsEx] bbb mood: {test.Key} {test.Value}");
                        //}

                        // jsonのpriority_weightsの上から順にとmerged_keysのキーと突き合わせて行く。
                        // priority_weightsと一致するもののみがmatched_priority_weightsに入る。
                        var matched_priority_weights = ExtractMatchedPriorityWeights(refs, merged_keys);

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
                                    mood_name = kvp.Value.filter_name;
                                    pic = true;
                                    break;
                                }
                            }

                            if (pic)
                            {
                                break;
                            }
                        }


                        //Log.Message($"[PortraitsEx] mood_name: {mood_name} ");

                        // 抽出した心情+社交値名がなければ、jsonのfallback_moodを使う。
                        if (mood_name == "")
                        {
                            if (refs.fallback_mood == "")
                            {
                                mood_name = "def";
                            }
                            else
                            {
                                mood_name = refs.fallback_mood;
                            }
                        }
                    }
                    //Log.Message($"[PortraitsEx] mood_name2: {mood_name} ");

                    if (mood_name != temp_refs_key)
                    {
                        // 心情+社交名と既に退避済みの心情+社交名が一致しないとき
                        Reset();
                        string access_key = "";

                        if (refs.MatchDictKeysByRegex(mood_name, out access_key))
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
                        if (nextPortrait)
                        {

                            return AdvanceToNextPortrait(def);
                        }
                        else
                        {

                            return GetCurrentPortrait(def);
                        }
                    }
                }
            }

            return def;
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

        private static Texture2D AdvanceToNextPortrait(Texture2D def)
        {
            if (temp.Count <= 0) return def;
            if (temp_index < temp.Count)
            {
                Texture2D tx = temp[temp_index];
                if (temp_animation_mode) ++temp_index;
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

        private static Dictionary<string, float> BuildAffectionImpactMap(Pawn pawn, out bool is_value_fetched)
        {
            // 別スレッドかどうか確認しておく ver1.6以降→要確認
            //Log.Message($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            is_value_fetched = false;
            Dictionary<string, float> affection_impact_map = new Dictionary<string, float>();
            List<Thought> outThoughts = new List<Thought>();
            var thoughts = pawn.needs?.mood?.thoughts;

            // メカノイドなどは心情を持たない
            if (thoughts == null) { return affection_impact_map; }

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

            is_value_fetched = true;

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

            return affection_impact_map;
        }

        private static List<KeyValuePair<string, string>> FilterGroupMatches(Refs refs, Dictionary<string, float> mood)
        {
            List<KeyValuePair<string, string>> filtered_group_filter = new List<KeyValuePair<string, string>>();
            foreach (var kvp in refs.group_filter)
            {
                bool match_found = false;

                if (refs.g_regex_cache.ContainsKey(kvp.Key))
                {
                    var reg = refs.g_regex_cache[kvp.Key];
                    foreach (var mood_key in mood.Keys)
                    {
                        if (reg.IsMatch(mood_key))
                        {
                            match_found = true;
                            break;
                        }
                    }

                    if (match_found)
                    {
                        filtered_group_filter.Add(kvp);
                    }
                }
                else
                {
                    if (mood.ContainsKey(kvp.Key)) match_found = true;

                    if (match_found)
                    {
                        filtered_group_filter.Add(kvp);
                    }
                }
            }

            return filtered_group_filter;
        }

        private static List<Dictionary<string, PriorityWeights>> ExtractMatchedPriorityWeights(Refs refs, Dictionary<string, string> merged_keys)
        {
            var matched_priority_weights = new List<Dictionary<string, PriorityWeights>>();
            foreach (var kp in refs.priority_weight_order)
            {
                bool match_found = false;

                var vp = refs.priority_weights[kp];
                Dictionary<string, PriorityWeights> dict = new Dictionary<string, PriorityWeights>();
                if (refs.pw_regex_cache.ContainsKey(kp))
                {
                    var reg = refs.pw_regex_cache[kp];
                    var lis = new List<string>();

                    foreach (var mk in merged_keys)
                    {
                        if (reg.IsMatch(mk.Key))
                        {
                            lis.Add(mk.Key);
                            match_found = true;
                            break;
                        }
                    }

                    if (match_found)
                    {
                        foreach (var elm in lis)
                        {
                            if (!dict.ContainsKey(elm))
                            {
                                dict.Add(elm, vp);
                            }
                        }
                    }
                }
                else
                {
                    if (merged_keys.ContainsKey(kp)) match_found = true;

                    if (match_found)
                    {
                        if (!dict.ContainsKey(kp))
                        {
                            dict.Add(kp, vp);
                        }
                    }
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
