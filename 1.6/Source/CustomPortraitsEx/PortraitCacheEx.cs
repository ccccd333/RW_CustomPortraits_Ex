using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository.PatternMatching;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository.RepeatRulesHelperClass;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;


namespace Foxy.CustomPortraits.CustomPortraitsEx
{

    public static class PortraitCacheEx
    {

        public static Dictionary<string, Refs> Refs = new Dictionary<string, Refs>(StringComparer.OrdinalIgnoreCase);

        public static InteractionSelectionMap InteractionSelectionMap = new InteractionSelectionMap();


        private static readonly string Setting = "Setting.json";

        private static DirectoryInfo RimWorldRootDirectory { get; } = new DirectoryInfo(GenFilePaths.ModsFolderPath).Parent;
        public static DirectoryInfo Directory { get; } = RimWorldRootDirectory.CreateSubdirectory("CustomPortraitsEx");

        public static DirectoryInfo PresetDirectory { get; } = Directory.CreateSubdirectory("Presets");
        public static DirectoryInfo RepeatRulesDirectory { get; } = Directory.CreateSubdirectory("RepeatRules");

        public static PExSetting Settings = new PExSetting();

        public static bool IsAvailable = false;

        public static Dictionary<string, List<string>> PresetErrorMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public static void Update()
        {
            Log.Message($"[PortraitsEx] Updating cache from directory: {Directory.FullName}");
            if (!Directory.Exists) Directory.Create();
            if (!PresetDirectory.Exists) PresetDirectory.Create();
            if (!RepeatRulesDirectory.Exists) RepeatRulesDirectory.Create();
            try
            {
                string json = File.ReadAllText(Directory.FullName + "/" + Setting);
                Settings = JsonConvert.DeserializeObject<PExSetting>(json);
                //Log.Message($"[PortraitsEx] Setting.json loaded: video_render_texture_width={Settings.video_render_texture_width}, video_render_texture_height={Settings.video_render_texture_height}");
            }
            catch (Exception)
            {
                Log.Error($"[PortraitsEx] The Setting.json file could not be loaded. : {Directory.FullName + "/Setting.json"}");
            }

            try
            {
                ReadDirectory(Directory);
            }
            catch (Exception)
            {
                Log.Warning("[PortraitsEx] Failed to load preset.");
                return;
            }

            if (Refs.Count > 0)
            {
                IsAvailable = true;
            }
        }

        public static List<string> ReadPresetJson(string preset_name)
        {
            // 再読取り側、mod設定ページから呼び出し

            List<string> error_message = new List<string>();

            if (Refs.TryGetValue(preset_name, out var oldRefs))
            {
                // 再読み込み時は古いTexture2Dを解放してからRemoveする
                foreach (var tx_pair in oldRefs.txs)
                {
                    foreach (var tex in tx_pair.Value.txs)
                    {
                        if (tex != null) UnityEngine.Object.Destroy(tex);
                    }
                }
                // videos は VideoEntry のみなので特別なリソース解放不要。
                // VideoPlayerManager はグローバルシングルトンなので止めるだけ。
                VideoPlayerManager.DestroyInstance();
                // プールキャッシュも破棄
                foreach (var cvp in oldRefs.cached_videos.Values)
                {
                    cvp.Cleanup();
                }
                oldRefs.cached_videos.Clear();
                oldRefs.videos.Clear();
                Refs.Remove(preset_name);
            }

            if (PresetErrorMap.ContainsKey(preset_name))
            {
                PresetErrorMap.Remove(preset_name);
            }

            if (preset_name == "InteractionFilter")
            {
                InteractionSelectionMap.InteractionFilter.Clear();
                InteractionSelectionMap.intf_regex_cache.Clear();
            }

            System.IO.FileInfo[] files = PresetDirectory.GetFiles($"{preset_name}.json", System.IO.SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                error_message.Add($"Preset JSON file not found: {preset_name}.json");
                return error_message;
            }

            try
            {
                JObject root = JObject.Parse(File.ReadAllText(@files[0].FullName));
                Refs r = new Refs();
                string repeat_rules_json_name = "";
                bool preset_loaded_successfully = true;
                // TODO:ここもリファクタリング必要・・・
                foreach (var token in root["conditions"])
                {
                    var mood_prop = (JProperty)token;
                    string key = mood_prop.Name;
                    JToken value = mood_prop.Value;
                    try
                    {
                        if (key == "fallback_mood")
                        {
                            if (value is JValue fallback_mood)
                            {
                                r.fallback_mood = fallback_mood.Value.ToString();

                            }

                        }
                        else if (key == "fallback_mood_on_death")
                        {
                            if (value is JValue fallback_mood_on_death)
                            {
                                r.fallback_mood_on_death = fallback_mood_on_death.Value.ToString();

                            }
                        }
                        else if (key == "refs")
                        {
                            Refts(preset_name, key, value, r);
                        }
                        else if (key == "interaction_filter")
                        {
                            InteractionFilter(preset_name, key, value, r);
                        }
                        else if (key == "group")
                        {
                            Group(preset_name, key, value, r);
                        }
                        else if (key == "priority_weights")
                        {
                            PriorityWeights(preset_name, key, value, r);
                        }
                        else if (key == "interrupt")
                        {
                            Interrupt(preset_name, key, value, r);
                        }
                        else if (key == "repeat_rules")
                        {
                            repeat_rules_json_name = GetRepeatRulesJsonName(value);
                        }
                        else
                        {
                            preset_loaded_successfully = false;
                            error_message.Add("The preset JSON definition is incorrect." + preset_name);
                        }
                    }
                    catch (Exception)
                    {
                        preset_loaded_successfully = false;
                        //error_message.Add("The preset JSON definition is incorrect." + preset_name + " [wt?]: " + e.Message);
                    }
                }
                //Log.Message($"[PortraitsEx] Result ==> Target preset: {preset_name} Refs Count: {r.txs.Count} Group Filter Count: {r.group_filter.Count} PriorityWeight Count: {r.priority_weights.Count}");
                if (!Refs.ContainsKey(preset_name))
                {
                    Refs.Add(preset_name, r);
                    if (preset_loaded_successfully && error_message.Count == 0 && !PresetErrorMap.ContainsKey(preset_name))
                    {
                        LoadRepeatRulesJson(preset_name, repeat_rules_json_name, r);
                        BuildVideoCache(preset_name, r);
                    }
                }
                else
                {
                    Log.Warning($"[PortraitsEx] Duplicate preset name detected. ==> Target preset: {preset_name}");
                }
            }
            catch (Exception e)
            {
                error_message.Add(e.Message);
            }

            if (Refs.Count > 0)
            {
                IsAvailable = true;
            }

            if (PresetErrorMap.ContainsKey(preset_name))
            {
                error_message.AddRange(PresetErrorMap[preset_name]);
            }

            return error_message;

        }

        private static void AddPresetLoadError(string preset_name, string error_message)
        {
            if (!PresetErrorMap.ContainsKey(preset_name))
            {
                PresetErrorMap[preset_name] = new List<string>();
            }

            PresetErrorMap[preset_name].Add(error_message);
        }

        /// <summary>
        /// 各プリセットの動画キーを priority_weights 順 → videos 順で列挙し、
        /// Setting.json の video_player_pool_limit をプリセット数で割った枚数分だけ
        /// CachedVideoPlayer を生成して VideoPlayerPool に登録する。
        /// 枚数に収まらない動画はシングルトン方式 (VideoPlayerManager) でフォールバック。
        /// </summary>
        private static void BuildVideoCache(string preset_name, Refs r)
        {
            int pool_limit = Settings.video_player_pool_limit;
            if (pool_limit <= 0 || r.videos.Count == 0) return;

            // ↓やめた、これだと最初のプリセットが動画を大量に持っていると、後続のプリセットが全くキャッシュされない。
            //// プリセット数で割り切り捨て（最低 1）
            //int presets_with_video = 0;
            //foreach (var kv in Refs)
            //{
            //    if (kv.Value.videos.Count > 0) presets_with_video++;
            //}
            //// 現在処理中の preset はまだ Refs に入っているので考慮済み
            //if (presets_with_video == 0) presets_with_video = 1;

            //int slots_per_preset = System.Math.Max(1, pool_limit / presets_with_video);


            // 優先順のリスト構築： priority_weights の動画キー → 残りの videos
            var ordered = new System.Collections.Generic.List<string>();
            foreach (var pw_key in r.priority_weight_order)
            {
                if (r.videos.ContainsKey(pw_key) && !ordered.Contains(pw_key))
                    ordered.Add(pw_key);
            }
            foreach (var v_key in r.videos.Keys)
            {
                if (!ordered.Contains(v_key))
                    ordered.Add(v_key);
            }

            int registered = 0;
            foreach (var video_key in ordered)
            {
                if (registered >= pool_limit) break;

                var ve = r.videos[video_key];
                string abs_path = Directory.FullName + "/" + ve.file_path;

                try
                {
                    var cvp = new CachedVideoPlayer(abs_path, ve.loop, ve.fallback_texture);
                    r.cached_videos[video_key] = cvp;
                    registered++;
                    Log.Message($"[PortraitsEx] VideoCache registered: preset={preset_name} key={video_key} path={abs_path}");
                }
                catch (Exception ex)
                {
                    Log.Error($"[PortraitsEx] VideoCache failed to create CachedVideoPlayer: preset={preset_name} key={video_key} ex={ex.Message}");
                }
            }

            Log.Message($"[PortraitsEx] VideoCache: preset={preset_name} registered={registered}/{r.videos.Count} (pool_limit={pool_limit})");
        }
        private static void ReadDirectory(DirectoryInfo directory)
        {
            Log.Message($"[PortraitsEx] Target directory: {PresetDirectory.FullName}");
            System.IO.FileInfo[] files = PresetDirectory.GetFiles("*.json", System.IO.SearchOption.TopDirectoryOnly);

            foreach (FileInfo file in files)
            {
                JObject root = JObject.Parse(File.ReadAllText(@file.FullName));
                string preset_name = root["preset_name"].ToString();
                Refs r = new Refs();
                string repeat_rules_json_name = "";
                bool preset_loaded_successfully = true;
                // TODO:やる気になったら、再読み込み時とjsonが読み取れない場合はテクスチャの削除をする
                // メモリリークしたっていう人がいれば優先で対応
                foreach (var token in root["conditions"])
                {
                    var mood_prop = (JProperty)token;
                    string key = mood_prop.Name;
                    JToken value = mood_prop.Value;
                    try
                    {
                        if (key == "fallback_mood")
                        {
                            if (value is JValue fallback_mood)
                            {
                                r.fallback_mood = fallback_mood.Value.ToString();

                            }

                        }
                        else if (key == "fallback_mood_on_death")
                        {
                            if (value is JValue fallback_mood_on_death)
                            {
                                r.fallback_mood_on_death = fallback_mood_on_death.Value.ToString();

                            }
                        }
                        else if (key == "refs")
                        {
                            Refts(preset_name, key, value, r);
                        }
                        else if (key == "interaction_filter")
                        {
                            InteractionFilter(preset_name, key, value, r);
                        }
                        else if (key == "group")
                        {
                            Group(preset_name, key, value, r);
                        }
                        else if (key == "priority_weights")
                        {
                            PriorityWeights(preset_name, key, value, r);
                        }
                        else if (key == "interrupt")
                        {
                            Interrupt(preset_name, key, value, r);
                        }
                        else if (key == "repeat_rules")
                        {
                            repeat_rules_json_name = GetRepeatRulesJsonName(value);
                        }
                        else
                        {
                            preset_loaded_successfully = false;
                            Log.Warning("The preset JSON definition is incorrect." + preset_name);
                        }
                    }
                    catch (Exception e)
                    {
                        preset_loaded_successfully = false;
                        Log.Warning("The preset JSON definition is incorrect." + preset_name + " [wt?]: " + e.Message);
                    }
                }
                Log.Message($"[PortraitsEx] Result ==> Target preset: {preset_name} Refs Count: {r.txs.Count} Group Filter Count: {r.group_filter.Count} PriorityWeight Count: {r.priority_weights.Count}");
                if (!Refs.ContainsKey(preset_name))
                {
                    Refs.Add(preset_name, r);
                    if (preset_loaded_successfully && !PresetErrorMap.ContainsKey(preset_name))
                    {
                        LoadRepeatRulesJson(preset_name, repeat_rules_json_name, r);
                        BuildVideoCache(preset_name, r);
                    }
                }
                else
                {
                    Log.Warning($"[PortraitsEx] Duplicate preset name detected. ==> Target preset: {preset_name}");
                }
            }
        }

        private static string GetRepeatRulesJsonName(JToken value)
        {
            if (value is JValue repeat_rules_json_name)
            {
                return repeat_rules_json_name.Value<string>() ?? "";
            }

            return "";
        }

        private static void LoadRepeatRulesJson(string preset_name, string repeat_rules_json_name, Refs r)
        {
            if (repeat_rules_json_name.NullOrEmpty())
            {
                return;
            }

            string file_name = Path.GetFileName(repeat_rules_json_name);
            if (Path.GetExtension(file_name).NullOrEmpty())
            {
                file_name += ".json";
            }

            string repeat_rules_json_path = Path.Combine(RepeatRulesDirectory.FullName, file_name);
            if (!File.Exists(repeat_rules_json_path))
            {
                AddPresetLoadError(preset_name, $"RepeatRules JSON file not found: {file_name}");
                return;
            }

            try
            {
                JObject root = JObject.Parse(File.ReadAllText(repeat_rules_json_path));
                //JToken repeat_rules_root = root["repeat_rules"] ?? root;
                LoadRepeatRulesRoot(root, r);
                r.repeat_rules.is_enabled = true;
                Log.Message($"[PortraitsEx] RepeatRules loaded ==> Target preset: {preset_name} File: {file_name}");
            }
            catch (Exception e)
            {
                AddPresetLoadError(preset_name, $"An error occurred while loading repeat_rules JSON: {file_name} {e.Message}");
            }
        }

        private static void LoadRepeatRulesRoot(JToken repeat_rules_root, Refs r)
        {
            if (!(repeat_rules_root is JObject repeat_rules_object))
            {
                throw new Exception("RepeatRules JSON root must be an object.");
            }

            List<string> valid_context_names = new List<string>(r.txs.Keys);
            foreach (var key in r.videos.Keys)
            {
                if (!valid_context_names.Contains(key))
                    valid_context_names.Add(key);
            }
            foreach (var key in r.cached_videos.Keys)
            {
                if (!valid_context_names.Contains(key))
                    valid_context_names.Add(key);
            }
            ValidationContext validation_context = new ValidationContext(valid_context_names);

            foreach (var context_token in repeat_rules_object)
            {
                string context_name = context_token.Key;
                if (!(context_token.Value is JObject repeat_object))
                {
                    throw new Exception($"RepeatRules context must be an object: {context_name}");
                }

             
                if (repeat_object.TryGetValue("loop", out JToken loop_token) && loop_token is JObject loop_object)
                {
                    RepeatLoopSettings repeat_loop_settings = new RepeatLoopSettings();

                    if (loop_object.TryGetValue("min_count", out JToken min_count_token))
                    {
                        repeat_loop_settings.min_count = min_count_token.Value<int>();
                    }

                    if (loop_object.TryGetValue("max_count", out JToken max_count_token))
                    {
                        repeat_loop_settings.max_count = max_count_token.Value<int>();
                    }

                    if (loop_object.TryGetValue("reset_max_count", out JToken reset_max_count_token))
                    {
                        repeat_loop_settings.reset_max_count = reset_max_count_token.Value<int>();
                        if (repeat_loop_settings.reset_max_count < 0)
                        {
                            throw new Exception("repeat_rules reset_max_count must be zero or greater.");
                        }
                    }

                    if (loop_object.TryGetValue("interrupt_contexts", out JToken interrupt_contexts) && interrupt_contexts is JArray interrupt_array)
                    {
                        foreach (var interrupt_context in interrupt_array)
                        {
                            repeat_loop_settings.interrupt_contexts.Add(interrupt_context.ToString());
                        }
                    }

                    r.repeat_rules.SetLoopSettings(context_name, repeat_loop_settings);
                }

                if (!repeat_object.TryGetValue("repeat_events", out JToken repeat_events_token) || !(repeat_events_token is JObject repeat_events_object))
                {
                    throw new Exception($"RepeatRules repeat_events must be an object: {context_name}");
                }

                foreach (var repeat_token in repeat_events_object)
                {
                    if (!int.TryParse(repeat_token.Key, out int repeat_index))
                    {
                        throw new Exception($"RepeatRules repeat index must be an integer: {context_name}.{repeat_token.Key}");
                    }

                    RepeatEvaluationGroup group = LoadRepeatEvaluationGroup(repeat_token.Value, validation_context);
                    r.repeat_rules.SetOperations(context_name, repeat_index, group);
                }
            }
        }

        private static RepeatEvaluationGroup LoadRepeatEvaluationGroup(JToken group_token, ValidationContext validation_context)
        {
            RepeatEvaluationGroup group = new RepeatEvaluationGroup();
            JToken operations_token = group_token;

            if (group_token is JObject group_object)
            {
                //if (group_object.TryGetValue("repeat_range_min", out JToken repeat_range_min))
                //{
                //    group.repeat_range_min = repeat_range_min.Value<int>();
                //}

                //if (group_object.TryGetValue("repeat_range_max", out JToken repeat_range_max))
                //{
                //    group.repeat_range_max = repeat_range_max.Value<int>();
                //}

                //if (group_object.TryGetValue("repeat_range", out JToken repeat_range) && repeat_range is JArray range_array && range_array.Count >= 2)
                //{
                //    group.repeat_range_min = range_array[0].Value<int>();
                //    group.repeat_range_max = range_array[1].Value<int>();
                //}

                if (group_object.TryGetValue("interrupt_contexts", out JToken interrupt_contexts) && interrupt_contexts is JArray interrupt_array)
                {
                    foreach (var interrupt_context in interrupt_array)
                    {
                        group.interrupt_contexts.Add(interrupt_context.ToString());
                    }
                }

                if (group_object.TryGetValue("operations", out JToken operations))
                {
                    operations_token = operations;
                }
            }

            if (!(operations_token is JArray operation_array))
            {
                throw new Exception("RepeatRules operations must be an array.");
            }

            foreach (var operation_token in operation_array)
            {
                OperationBase operation = CreateRepeatRulesOperation(operation_token, validation_context);
                group.operation_list.Add(operation);
            }

            return group;
        }

        private static OperationBase CreateRepeatRulesOperation(JToken operation_token, ValidationContext validation_context)
        {
            if (!(operation_token is JObject operation_object))
            {
                throw new Exception("Variant operation must be an object.");
            }

            Operation operation = new Operation();

            string operation_type = operation_object.Value<string>("operation_type") ?? "";
            if (!Enum.TryParse(operation_type, out operation.operation_type))
            {
                throw new Exception($"Unknown repeat_rules operation_type: {operation_type}");
            }

            string inequality_sign = operation_object.Value<string>("inequality_sign") ?? "def";
            if (!Enum.TryParse(inequality_sign, out operation.inequality_sign))
            {
                operation.inequality_sign = InequalitySign.unk;
            }

            // operation_base_value が JObject の場合は複合条件。ローカルで保持し InitWithObject に直接渡す（Operation には格納しない）
            var base_value_token = operation_object["operation_base_value"] ?? operation_object["value"];
            JObject base_value_as_object = base_value_token as JObject;
            if (base_value_as_object == null)
            {
                operation.operation_base_value = base_value_token?.Value<string>() ?? "";
            }
            operation.override_portrait_name = operation_object.Value<string>("override_portrait_name") ?? operation_object.Value<string>("result_context_name") ?? "";
            if (operation_object.TryGetValue("override_min_count", out JToken override_min_count_token))
            {
                int override_min_count = override_min_count_token.Value<int>();
                if (override_min_count < 0)
                {
                    throw new Exception("repeat_rules override_min_count must be zero or greater.");
                }

                operation.override_min_count = override_min_count;
            }

            if (operation_object.TryGetValue("override_max_count", out JToken override_max_count_token))
            {
                int override_max_count = override_max_count_token.Value<int>();
                if (override_max_count < 0)
                {
                    throw new Exception("repeat_rules override_max_count must be zero or greater.");
                }

                operation.override_max_count = override_max_count;
            }

            if (operation_object.TryGetValue("override_reset_max_count", out JToken override_reset_max_count_token))
            {
                int override_reset_max_count = override_reset_max_count_token.Value<int>();
                if (override_reset_max_count < 0)
                {
                    throw new Exception("repeat_rules override_reset_max_count must be zero or greater.");
                }

                operation.override_reset_max_count = override_reset_max_count;
            }

            OperationBase result;
            switch (operation.operation_type)
            {
                case OperationType.portrait_context_name:
                    result = new PortraitContextName();
                    break;
                case OperationType.rand_value:
                    result = new RandValue();
                    break;
                case OperationType.last_context_name:
                    result = new LastContextName();
                    break;
                case OperationType.last_context_and_rand:
                    result = new LastContextAndRand();
                    break;
                default:
                    throw new Exception($"Unsupported repeat_rules operation_type: {operation.operation_type}");
            }

            bool init_ok;
            if (base_value_as_object != null)
            {
                // JObject は Init 後に不要なのでローカル変数のスコープ内のみで参照される
                init_ok = result.InitWithObject(operation, base_value_as_object, validation_context);
            }
            else
            {
                init_ok = result.Init(operation, validation_context);
            }

            if (!init_ok)
            {
                throw new Exception($"Failed to initialize repeat_rules operation: {operation.operation_type}");
            }

            return result;
        }

        private static void Refts(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] Refts ==> Target preset: {preset_name}");

            foreach (var token in n)
            {
                var prop = (JProperty)token;

                string Refs_key = prop.Name;

                //Log.Message($"[PortraitsEx] Refts ==> Target preset: {preset_name} ==> {Refs_key}");
                JToken value = prop.Value;

                foreach (var token_n in value)
                {
                    var prop_n = (JProperty)token_n;
                    string cont = prop_n.Name;

                    //Log.Message($"[PortraitsEx] Refts ==> Target preset: {preset_name} ==> {Refs_key} ==> {cont}");

                    if (cont == "textures")
                    {
                        Log.Message($"[PortraitsEx] Texture Key ==> Target preset: {preset_name} ==> {Refs_key} ==> {cont}");
                        var tx = Textures(preset_name, Refs_key, cont, prop_n.Value, r);
                        r.txs.Add(Refs_key, tx);
                    }
                    else if (cont == "video")
                    {
                        // mp4 動画エントリを読み込む。
                        // textures と並存可能。同一キーに両方あれば video を優先する（ConditionDrivenPortrait 側で制御）。
                        Log.Message($"[PortraitsEx] Video Key ==> Target preset: {preset_name} ==> {Refs_key} ==> {cont}");
                        var ve = LoadVideoEntry(preset_name, Refs_key, prop_n.Value);
                        if (ve != null && !r.videos.ContainsKey(Refs_key))
                            r.videos.Add(Refs_key, ve);
                    }

                }

            }
        }

        /// <summary>
        /// JSON の video エントリをパースして VideoEntry を返す。
        /// ファイルが見つからない場合は null を返す。
        /// </summary>
        private static VideoEntry LoadVideoEntry(string preset_name, string refs_key, JToken n)
        {
            VideoEntry ve = new VideoEntry();
            try
            {
                foreach (var token in n)
                {
                    var prop = (JProperty)token;
                    string conf = prop.Name;

                    if (conf == "display_duration")
                    {
                        if (prop.Value is JValue disp)
                            ve.display_duration = disp.Value<float>();
                    }
                    else if (conf == "loop")
                    {
                        if (prop.Value is JValue loopVal)
                            ve.loop = loopVal.Value<bool>();
                    }
                    else if (conf == "files")
                    {
                        // VideoPlayer は 1 個だけなので先頭のエントリのみ使用。
                        var arr = (JArray)prop.Value;
                        if (arr.Count > 0)
                            ve.file_path = arr[0].ToString();
                    }
                    else if (conf == "fallback_texture")
                    {
                        if (prop.Value is JValue fbVal)
                            ve.fallback_texture_path = fbVal.Value<string>();
                    }
                }

                if (ve.file_path == "")
                {
                    AddPresetLoadError(preset_name, $"[video] refs key '{refs_key}' has no 'files' defined.");
                    return null;
                }

                // ファイル存在チェック
                string abs_path = Directory.FullName + "/" + ve.file_path;
                if (!System.IO.File.Exists(abs_path))
                {
                    AddPresetLoadError(preset_name, $"[video] File not found: {abs_path}");
                    return null;
                }

                // フォールバックテクスチャのロード
                if (!string.IsNullOrEmpty(ve.fallback_texture_path))
                {
                    string fb_path = Directory.FullName + "/" + ve.fallback_texture_path;
                    if (System.IO.File.Exists(fb_path))
                    {
                        try
                        {
                            byte[] data = File.ReadAllBytes(fb_path);
                            string ext = Path.GetExtension(fb_path).ToLower();
                            if (ext == ".dds")
                            {
                                ve.fallback_texture = LoadTextureDDS(data);
                            }
                            else
                            {
                                ve.fallback_texture = new Texture2D(2, 2);
                                ve.fallback_texture.LoadImage(data);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"[PortraitsEx] [video] Failed to load fallback_texture '{fb_path}': {e.Message}");
                        }
                    }
                    else
                    {
                        Log.Warning($"[PortraitsEx] [video] fallback_texture not found: {fb_path}");
                    }
                }

                Log.Message($"[PortraitsEx] Video Entry loaded ==> preset: {preset_name} key: {refs_key} path: {abs_path} loop: {ve.loop}");
                return ve;
            }
            catch (Exception e)
            {
                AddPresetLoadError(preset_name, $"[video] Error loading video entry for '{refs_key}': {e.Message}");
                return null;
            }
        }

        private static void InteractionFilter(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] InteractionFilter ==> Target preset: {preset_name}");
            try
            {
                foreach (var token in n)
                {
                    InteractionFilter intf = new InteractionFilter();
                    var prop = (JProperty)token;

                    string intf_key = prop.Name;

                    //Log.Message($"[PortraitsEx] Refts ==> Target preset: {preset_name} ==> {Refs_key}");
                    JToken value = prop.Value;

                    foreach (var token_n in value)
                    {
                        var prop_n = (JProperty)token_n;
                        string cont = prop_n.Name;

                        //Log.Message($"[PortraitsEx] Refts ==> Target preset: {preset_name} ==> {Refs_key} ==> {cont}");

                        if (cont == "is_recipient")
                        {
                            if (prop_n.Value is JValue is_recipient)
                            {
                                intf.is_recipient = is_recipient.Value<int>() == 1 ? true : false;
                            }
                        }
                        else if (cont == "matched_recipient_key")
                        {
                            if (prop_n.Value is JValue matched_recipient_key)
                            {
                                intf.matched_recipient_key = matched_recipient_key.Value<string>() ?? "";
                            }
                        }
                        else if (cont == "is_initiator")
                        {
                            if (prop_n.Value is JValue is_initiator)
                            {
                                intf.is_initiator = is_initiator.Value<int>() == 1 ? true : false;
                            }
                        }
                        else if (cont == "matched_initiator_key")
                        {
                            if (prop_n.Value is JValue matched_initiator_key)
                            {
                                intf.matched_initiator_key = matched_initiator_key.Value<string>() ?? "";
                            }
                        }
                        else if (cont == "cache_duration_seconds")
                        {
                            if (prop_n.Value is JValue cache_duration_seconds)
                            {
                                float val;
                                if (!float.TryParse(cache_duration_seconds.ToString(), out val))
                                {
                                    val = 12.0f;
                                }
                                intf.cache_duration_seconds = val;
                            }
                        }

                    }
                    intf.interaction_name = intf_key;
                    InteractionSelectionMap.InteractionFilter.Add(intf_key, intf);
                    if (Utility.IsRegexPattern(intf_key))
                    {
                        //Log.Message($"[PortraitsEx] InteractionFilter ==> Target preset: {preset_name} ADDREGEX");
                        InteractionSelectionMap.intf_regex_cache.Add(intf_key, PatternMatcherFactory.Create(intf_key));
                    }
                    Log.Message($"[PortraitsEx] InteractionFilter ==> Target preset: {preset_name} Key: {intf_key} matched_initiator_key: {intf.matched_initiator_key} matched_recipient_key: {intf.matched_recipient_key}");
                }
            }
            catch (Exception e)
            {
                AddPresetLoadError(preset_name, "An error occurred while loading InteractionFilter. Please review the JSON that defines \"interaction_filter\" in the preset.");
                throw e;
            }
        }
        private static void Group(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] Group ==> Target preset: {preset_name}");
            try
            {
                Dictionary<string, List<string>> group_alias_map = new Dictionary<string, List<string>>();

                foreach (var token in n)
                {
                    var prop = (JProperty)token;

                    string g_k = prop.Name;

                    JToken value = prop.Value;

                    foreach (var v in (JArray)prop.Value)
                    {
                        var Refs_key = v.ToString();
                        if (Refs_key.StartsWith("@"))
                        {
                            string ref_group_key = Refs_key.Substring(1);
                            if (!group_alias_map.TryGetValue(ref_group_key, out var list))
                            {
                                list = new List<string>();
                                group_alias_map[ref_group_key] = list;
                            }

                            list.Add(g_k);
                        }
                        else if (!r.group_filter.ContainsKey(Refs_key))
                        {
                            r.group_filter.Add(Refs_key, new GroupPatternEntry(g_k));
                            if (Utility.IsRegexPattern(Refs_key))
                            {
                                r.g_regex_cache.Add(Refs_key, PatternMatcherFactory.Create(Refs_key));
                            }

                            Log.Message($"[PortraitsEx] Group ==> Target preset: {preset_name} Group Key ==> {g_k} Value ==> {Refs_key}");
                        }
                    }
                }

                // 参照関係の解決
                if (group_alias_map.Count > 0)
                {
                    foreach (var group in r.group_filter)
                    {
                        if (group_alias_map.TryGetValue(group.Value.key, out var list))
                        {
                            group.Value.alias_targets = list;
                            Log.Message($"[PortraitsEx] CrossRef Group ==> Group Key ==> {group.Value.key} Aliases ==> {string.Join(", ", list)}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AddPresetLoadError(preset_name, "An error occurred while loading the preset Group key. Please review the JSON that defines \"Group\" in the preset.");
                throw e;
            }
        }

        private static void PriorityWeights(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] PriorityWeights ==> Target preset: {preset_name}");
            try
            {
                foreach (var v in n)
                {
                    PriorityWeights pw = new PriorityWeights();
                    var obj = (JProperty)v;
                    string Refs_key = obj.Name;

                    JToken wvalue = obj.Value;

                    pw.filter_name = Refs_key;
                    foreach (var vvv in wvalue)
                    {
                        var nw = (JProperty)vvv;
                        string nkey = nw.Name;
                        JToken nvalue = nw.Value;

                        if (nkey == "category")
                        {
                            if (nvalue is JValue category)
                            {
                                pw.category = (PriorityWeightCategory)Enum.ToObject(typeof(PriorityWeightCategory), category.Value<int>());
                            }
                        }
                        else if (nkey == "weight")
                        {

                            if (nvalue is JValue weight)
                            {
                                pw.weight = weight.Value<int>();
                            }
                        }
                        else
                        {
                            throw new Exception("The preset JSON definition is incorrect." + preset_name);
                        }
                    }
                    //Log.Message($"[PortraitsEx] PriorityWeights ==> Target preset: {preset_name} filter_name: {pw.filter_name} weight: {pw.weight}");
                    if (r.priority_weights.ContainsKey(Refs_key))
                    {
                        Log.Message($"[PortraitsEx] Duplicate priority weights detected. ==> Target preset: {preset_name} Duplicate Key: {Refs_key}");
                    }
                    else
                    {
                        r.priority_weights.Add(Refs_key, pw);
                        r.priority_weight_order.Add(Refs_key);
                        //if (Utility.IsRegexPattern(Refs_key))
                        //{
                        //    r.pw_regex_cache.Add(Refs_key, new Regex(Refs_key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                        //}
                    }

                }
            }
            catch (Exception e)
            {
                AddPresetLoadError(preset_name, "An error occurred while loading the priority_weights key in the preset. Please review the JSON that defines \"priority_weights\" in the preset.");
                throw e;
            }


        }

        private static void Interrupt(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] Interrupt ==> Target preset: {preset_name}");
            try
            {
                PortraitInterrupt interr = new PortraitInterrupt();
                bool enabled_monitor = false;
                foreach (var v in n)
                {

                    var obj = (JProperty)v;
                    string Refs_key = obj.Name;

                    JToken intrrvalue = obj.Value;


                    if (Refs_key == "monitors")
                    {

                        if (intrrvalue is JArray monitors_array)
                        {
                            foreach (var monitors_value in monitors_array)
                            {
                                string monitor_value = monitors_value.ToString();

                                if (monitor_value == PortraitContextKeys.PAIN_INCREASE)
                                {
                                    interr.enabled_monitors[(int)MonitorType.PainIncrease] = true;
                                    enabled_monitor = true;
                                }
                                else if (monitor_value == PortraitContextKeys.DOWNED)
                                {
                                    interr.enabled_monitors[(int)MonitorType.Downed] = true;
                                    enabled_monitor = true;

                                }
                            }
                        }
                    }
                    else if (Refs_key == "monitor_behaviors")
                    {
                        interr.monitor_behaviors.LoadFromJson(intrrvalue);
                    }
                    else if (Refs_key == "group")
                    {
                        foreach (var iv in intrrvalue)
                        {
                            var prop = (JProperty)iv;

                            string g_k = prop.Name;

                            JToken value = prop.Value;

                            foreach (var iiv in (JArray)prop.Value)
                            {
                                var iRefs_key = iiv.ToString();
                                if (!interr.group_filter.ContainsKey(iRefs_key))
                                {
                                    interr.group_filter.Add(iRefs_key, new GroupPatternEntry(g_k));

                                    Log.Message($"[PortraitsEx] Group ==> Target preset: {preset_name} Group Key ==> {g_k} Value ==> {Refs_key}");
                                }
                            }
                        }
                    }
                    else if (Refs_key == "priority_weights")
                    {
                        foreach (var iv in intrrvalue)
                        {
                            PriorityWeights pw = new PriorityWeights();
                            var iobj = (JProperty)iv;
                            string iRefs_key = iobj.Name;

                            JToken wvalue = iobj.Value;

                            pw.filter_name = iRefs_key;
                            foreach (var vvv in wvalue)
                            {
                                var nw = (JProperty)vvv;
                                string nkey = nw.Name;
                                JToken nvalue = nw.Value;

                                if (nkey == "category")
                                {
                                    if (nvalue is JValue category)
                                    {
                                        pw.category = (PriorityWeightCategory)Enum.ToObject(typeof(PriorityWeightCategory), category.Value<int>());
                                    }
                                }
                                else if (nkey == "weight")
                                {

                                    if (nvalue is JValue weight)
                                    {
                                        pw.weight = weight.Value<int>();
                                    }
                                }
                                else
                                {
                                    throw new Exception("The preset JSON definition is incorrect." + preset_name);
                                }
                            }

                            if (interr.priority_weights.ContainsKey(iRefs_key))
                            {
                                Log.Message($"[PortraitsEx] Duplicate priority weights detected. ==> Target preset: {preset_name} Duplicate Key: {iRefs_key}");
                            }
                            else
                            {
                                interr.priority_weights.Add(iRefs_key, pw);
                                interr.priority_weight_order.Add(iRefs_key);
                            }

                        }
                    }
                    else
                    {
                        throw new Exception("The preset JSON definition is incorrect." + preset_name);
                    }

                }

                if (enabled_monitor)
                {
                    interr.interrupt_enabled = true;
                    r.interrupt = interr;
                }
            }
            catch (Exception e)
            {
                AddPresetLoadError(preset_name, "An error occurred while loading the priority_weights key in the preset. Please review the JSON that defines \"priority_weights\" in the preset.");
                throw e;
            }
        }


        private static Textures Textures(string preset_name, string refs_key, string k, JToken n, Refs r)
        {
            //Log.Message($"[PortraitsEx] Textures ==> Target preset: {preset_name}");

            Textures tx = new Textures();
            try
            {
                if(PresetErrorMap.TryGetValue(preset_name, out var list) && list.Count > 0)
                {
                    Log.Message($"[PortraitsEx] Textures ==> Target preset: {preset_name} has errors. Skip loading textures.");
                    return tx;
                }

                foreach (var token in n)
                {
                    var prop = (JProperty)token;

                    // todo:もし評価基準が増えればtypeとともに処理を作る
                    string conf = prop.Name;
                    if (conf == "animation_mode")
                    {
                        if (prop.Value is JValue animation_mode)
                        {
                            tx.IsAnimation = animation_mode.Value<int>() == 0 ? false : true;
                        }
                    }
                    else if (conf == "display_duration")
                    {
                        if (prop.Value is JValue display_duration)
                        {
                            tx.display_duration = display_duration.Value<float>();
                        }
                    }
                    else if (conf == "files")
                    {

                        foreach (var v in (JArray)prop.Value)
                        {
                            string portrait_path = v.ToString();
                            tx.file_path = portrait_path;

                            if (portrait_path.Contains("~"))
                            {
                                string[] parts = portrait_path.Split('~');

                                string first = parts[0];
                                string second = parts[1];
                                string base_path = first.Substring(0, first.LastIndexOf('/') + 1);
                                string first_file = first.Substring(base_path.Length);
                                string second_file = second;
                                int range_from = 0;
                                int range_to = 0;
                                string d = "";
                                if (!int.TryParse(Utility.DDelimiter(first_file, out d), out range_from))
                                {
                                    throw new Exception($"Please make sure to use the DDS (DXT1) format for loading images used in animation." + preset_name + "." + k);
                                }
                                if (!int.TryParse(Utility.DDelimiter(second_file, out d), out range_to))
                                {
                                    throw new Exception($"Please make sure to use the DDS (DXT1) format for loading images used in animation." + preset_name + "." + k);
                                }

                                if (d == "")
                                {
                                    Log.Error($"[PortraitsEx] Portrait Load Error: Only the DDS(DXT1) image format is supported.");
                                    throw new Exception($"Failed to load image. Processing will end." + preset_name + "." + k);
                                }

                                if (range_from > range_to)
                                {
                                    int escp = range_to;
                                    range_to = range_from;
                                    range_from = escp;
                                }

                                tx.d = d.ToLower();
                                tx.file_base_path = base_path;
                                tx.file_path_first = range_from.ToString();
                                tx.file_path_second = range_to.ToString();
                                tx.file_path = tx.file_base_path + tx.file_path_first + tx.d + "~" + tx.file_path_second + tx.d;
                                //Log.Message($"[PortraitsEx] Load Txture ==> {tx.file_path}");
                                for (; range_from <= range_to; range_from++)
                                {
                                    string f = Directory.FullName + "/" + base_path + range_from.ToString() + d;
                                    try
                                    {

                                        byte[] data = File.ReadAllBytes(f);
                                        Texture2D tex = LoadTextureDDS(data);

                                        tx.txs.Add(tex);
                                    }
                                    catch (Exception e)
                                    {
                                        throw new Exception($"Texture not found. file_path ==>{f} wt?==>{e.Message}");
                                    }
                                }

                            }
                            else
                            {
                                string d = "";
                                Utility.Delimiter(portrait_path, out d);
                                tx.d = d.ToLower();
                                tx.file_base_path = portrait_path.Substring(0, portrait_path.LastIndexOf('/') + 1);
                                tx.file_path_first = portrait_path.Substring(tx.file_base_path.Length);

                                tx.file_path = tx.file_base_path + tx.file_path_first + tx.d;
                                //Log.Message($"[PortraitsEx] Load Txture ==> {tx.file_path}");
                                if (d.ToLower() == ".dds")
                                {
                                    string f = Directory.FullName + "/" + v;
                                    try
                                    {
                                        byte[] data = File.ReadAllBytes(f);
                                        Texture2D tex = LoadTextureDDS(data);
                                        tx.txs.Add(tex);
                                    }
                                    catch (Exception e)
                                    {
                                        throw new Exception($"Texture not found. file_path ==>{f} wt?==>{e.Message}");
                                    }
                                }
                                else
                                {
                                    string f = Directory.FullName + "/" + v;
                                    try
                                    {
                                        byte[] data = File.ReadAllBytes(f);
                                        Texture2D tex = new Texture2D(2, 2);
                                        tex.LoadImage(data);
                                        tx.txs.Add(tex);
                                    }
                                    catch (Exception e)
                                    {
                                        throw new Exception($"Texture not found. file_path ==>{f} wt?==>{e.Message}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("The preset JSON definition is incorrect." + preset_name + "." + k);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[PortraitsEx] Texture Load Error: DeleteTextureList ==>");

                if (r.txs.Count > 0)
                {
                    foreach (var tex_pair in r.txs)
                    {
                        foreach (var tex in tex_pair.Value.txs)
                        {
                            if (tex != null)
                            {
                                UnityEngine.Object.Destroy(tex);
                            }
                        }
                        Log.Message($"DeleteTexture ==>{tex_pair.Key} {tex_pair.Value.file_base_path}: {tex_pair.Value.file_path_first} ~ {tex_pair.Value.file_path_second}");
                    }

                    r.txs.Clear();
                }

                // 既にロード済みのテクスチャを解放
                if (tx.txs.Count > 0)
                {
                    foreach (var tex in tx.txs)
                    {
                        if (tex != null)
                        {
                            UnityEngine.Object.Destroy(tex);
                        }
                    }

                    Log.Message($"DeleteTexture ==>{refs_key} {tx.file_base_path}: {tx.file_path_first} ~ {tx.file_path_second}");

                    tx.txs.Clear();
                }

                AddPresetLoadError(preset_name, e.Message);
                throw e;
            }

            return tx;
        }

        private static Texture2D LoadTextureDDS(byte[] data)
        {
            DDS dds = new DDS(data);

            Texture2D tex = new Texture2D((int)dds.Width, (int)dds.Height, dds.Format, false);
            if (dds.Format == TextureFormat.DXT1)
                Utility.FlipDXT1(dds.DXT, (int)dds.Width, (int)dds.Height);
            else
                Utility.FlipDXT5(dds.DXT, (int)dds.Width, (int)dds.Height);
            tex.LoadRawTextureData(dds.DXT);
            tex.Apply(false, true);

            return tex;
        }


    }
}
