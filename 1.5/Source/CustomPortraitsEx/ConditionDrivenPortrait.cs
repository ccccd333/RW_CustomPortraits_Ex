
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
        private static float frame_interval_seconds = 0.1f;
        private static bool portrait_skip_on_lag = true;

        private static float disp_last_update_time = Time.realtimeSinceStartup;

        private static bool is_interrupt_active = false;

        public static void Reset()
        {
            // ゲームロード開始時などに入ってくる

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

                bool next_portrait = false;
                int skip_count = 1;
                // ゲーム内時間だとFPSに依存してしまうのでUnityの内部タイマーでフレーム計算する
                float current_time = Time.realtimeSinceStartup;

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

                if (mood_refs.ContainsKey(preset_name) && !PortraitCacheEx.PresetErrorMap.ContainsKey(preset_name))
                {


                    if (temp_preset_name != preset_name)
                    {
                        // ポートレートが別々のポーンの場合、退避情報をクリアして、後続処理をする。
                        Reset();
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

                        bool intr_is_value_fetched = false;
                        Dictionary<string, float> intr_impact_map = new Dictionary<string, float>();
                        if (refs.interrupt.interrupt_enabled)
                        {
                            // 割り込み計算。あまり大きくなるようなら別スレッド。
                            intr_impact_map = PawnPortraitInterruptContext.ComposeImpactMap(pawn, refs.interrupt, is_interrupt_active, out intr_is_value_fetched);
                        }

                        if (!is_interrupt_active && intr_is_value_fetched)
                        {
                            // 割り込みが発生した場合、割り込みフラグをtrueにする。
                            // 以降割り込みはtemp_display_durationまで、タイマー処理を通常運行側で処理させる。
                            // そのためその間は割り込みは発生しない
                            // temp_display_duration過ぎた後に平常運転になったら、再度割り込みフラグがfalseになる
                            // なので割り込み→割り込みはなく、割り込み→平常運転→平常運転キャンセル割り込みとなる。
                            // ダメージ系はスパンが短いものは違和感ないはず。
                            // 割り込みのクールタイムを考えたがそれならtemp_display_durationに委ねたほうがユーザー側で操作しやすい
                            is_interrupt_active = true;
                            // 割り込み時点から計算するためtemp_display_durationを現在時刻で初期化
                            disp_last_update_time = Time.realtimeSinceStartup;


                            bool is_resolved = false;
                            bool no_match = false;
                            portrait_context_name = ResolveInterruptPortraitContextName(pawn, refs, intr_impact_map, out is_resolved, out no_match);
                            if (!is_resolved)
                            {
                                // 割り込み用のキーグループは書いてあるけど、空の場合とか
                                // 本来の運用から外れている場合
                                Log.Error("[PortraitsEx] ResolveInterruptPortraitContextName ===> no result.");
                                return def;
                            }

                            if(no_match && PortraitCacheEx.Settings.interrupt_fallback_to_steady)
                            {
                                // ポーンが生きていて、割り込み処理(ダメージ受けた系)ではない場合は
                                // こちらでJson内容とゲームパラメータで突き合わせる
                                bool is_value_fetched = false;
                                Dictionary<string, float> impact_map = PawnPortraitContext.ComposeImpactMap(pawn, out is_value_fetched);

                                if (!is_value_fetched)
                                {
                                    // 途中で処理を抜けた(心情取れないとか)の場合、デフォルトの画像キャッシュを返却
                                    // ただ、ゲーム内のものなので本来ないはず
                                    Reset();
                                    return def;
                                }

                                portrait_context_name = ResolveSteadyPortraitContextName(pawn, refs, impact_map, out is_resolved);
                                if (!is_resolved)
                                {
                                    // こちらはそもそも突き合わせたけど何もないやつ
                                    // ないはず。一応監視。
                                    Log.Error("[PortraitsEx][INTERRUPT FALLBACK] ResolveSteadyPortraitContextName ===> no result.");
                                    return def;
                                }

                                //Log.Message($"[PortraitsEx][INTERRUPT FALLBACK] name ==> {portrait_context_name}");
                            }

                            //Log.Message($"[PortraitsEx] Interrupt ==> portrait_context_name {portrait_context_name}");
                        }
                        else
                        {

                            if (temp_preset_name == preset_name)
                            {
                                //Log.Message($"[PortraitsEx] disp_last_update_time ==> {disp_last_update_time} current_time ==> {current_time} temp_display_duration ==> {temp_display_duration}");

                                // 毎回後続の重い処理を実行したくないのでjsonのdisplay_durationの間は退避した情報で
                                // アニメーションor画像表示を行う。
                                // temp_display_duration(jsonの時間定義)を超えたら次のやつを探しに後続処理をする
                                if (current_time - disp_last_update_time <= temp_display_duration)
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
                                else
                                {
                                    disp_last_update_time = Time.realtimeSinceStartup;
                                }
                            }

                            if (!is_interrupt_active)
                            {
                                // ポーンが生きていて、割り込み処理(ダメージ受けた系)ではない場合は
                                // こちらでJson内容とゲームパラメータで突き合わせる
                                bool is_resolved = false;
                                bool is_value_fetched = false;
                                Dictionary<string, float> impact_map = PawnPortraitContext.ComposeImpactMap(pawn, out is_value_fetched);

                                if (!is_value_fetched)
                                {
                                    // 途中で処理を抜けた(心情取れないとか)の場合、デフォルトの画像キャッシュを返却
                                    // ただ、ゲーム内のものなので本来ないはず
                                    Reset();
                                    return def;
                                }

                                portrait_context_name = ResolveSteadyPortraitContextName(pawn, refs, impact_map, out is_resolved);
                                if (!is_resolved)
                                {
                                    // こちらはそもそも突き合わせたけど何もないやつ
                                    // ないはず。一応監視。
                                    Log.Error("[PortraitsEx] ResolveSteadyPortraitContextName ===> no result.");
                                    return def;
                                }
                            }
                            else
                            {
                                // 割り込みが起きた際は割り込み→平常→割り込みと1フレームで切り替わる可能性があるので
                                // 一旦portrait_context_nameにtemp_refs_keyを入れる。
                                portrait_context_name = temp_refs_key;
                            }
                            // 完全に平常運転になったので割り込みフラグをfalseにする
                            is_interrupt_active = false;
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
            var filtered_group_filter = FilterGroupMatches(refs.g_regex_cache, refs.group_filter, impact_map);

            // 心情+社交などが一切ない。
            if (filtered_group_filter.Count() <= 0 && impact_map.Count() <= 0)
            {
                return "";
            }


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
            foreach (var kvp in impact_map)
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
            var matched_priority_weights = ExtractMatchedPriorityWeights(refs.priority_weight_order, refs.pw_regex_cache, refs.priority_weights, merged_keys);

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
            //    Log.Message($"[PortraitsEx] refs.interrupt.group_filter ==> Key {k.Key} Value {k.Value}");
            //}

            //foreach (var k in impact_map)
            //{
            //    Log.Message($"[PortraitsEx] impact_map ==> Key {k.Key} Value {k.Value}");
            //}


            // jsonのグループのキー(Group名)と値(心情+社交名)mood_and_last_social(心情+社交の文字と値)のキーと一致する場合
            // filtered_group_filterに一旦重複してもいいので入れていく。
            var filtered_group_filter = FilterGroupMatches(null, refs.interrupt.group_filter, impact_map);

            // 心情+社交が一切ない(?)。
            if (filtered_group_filter.Count() <= 0 && impact_map.Count() <= 0)
            {
                return "";
            }


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
            foreach (var kvp in impact_map)
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
            var matched_priority_weights = ExtractMatchedPriorityWeights(refs.interrupt.priority_weight_order, null, refs.interrupt.priority_weights, merged_keys);

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

        private static List<KeyValuePair<string, string>> FilterGroupMatches(
            Dictionary<string, Regex> g_regex_cache,
            Dictionary<string, string> group_filter,
            Dictionary<string, float> mood)
        {
            List<KeyValuePair<string, string>> filtered_group_filter = new List<KeyValuePair<string, string>>();
            foreach (var kvp in group_filter)
            {
                bool match_found = false;

                if (g_regex_cache != null && g_regex_cache.ContainsKey(kvp.Key))
                {
                    var reg = g_regex_cache[kvp.Key];
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

        private static List<Dictionary<string, PriorityWeights>> ExtractMatchedPriorityWeights(
            List<string> priority_weight_order,
            Dictionary<string, Regex> pw_regex_cache,
            Dictionary<string, PriorityWeights> priority_weights,
            Dictionary<string, string> merged_keys)
        {
            var matched_priority_weights = new List<Dictionary<string, PriorityWeights>>();
            foreach (var kp in priority_weight_order)
            {
                bool match_found = false;

                var vp = priority_weights[kp];
                Dictionary<string, PriorityWeights> dict = new Dictionary<string, PriorityWeights>();
                if (pw_regex_cache != null && pw_regex_cache.ContainsKey(kp))
                {
                    var reg = pw_regex_cache[kp];
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
