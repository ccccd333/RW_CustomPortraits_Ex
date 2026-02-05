using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
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

        public static PExSetting Settings = new PExSetting();

        public static bool IsAvailable = false;

        public static Dictionary<string, List<string>> PresetErrorMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        public static void Update()
        {
            Log.Message($"[PortraitsEx] Updating cache from directory: {Directory.FullName}");
            if (!Directory.Exists) Directory.Create();
            if (!PresetDirectory.Exists) PresetDirectory.Create();
            try
            {
                ReadDirectory(Directory);
            }
            catch (Exception)
            {
                Log.Warning("[PortraitsEx] Failed to load preset.");
                return;
            }

            try
            {
                string json = File.ReadAllText(Directory.FullName + "/" + Setting);
                Settings = JsonConvert.DeserializeObject<PExSetting>(json);
            }
            catch (Exception)
            {
                Log.Error($"[PortraitsEx] The Setting.json file could not be loaded. : {Directory.FullName + "/Setting.json"}");
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

            // TODO:やる気になったら、再読み込み時とjsonが読み取れない場合はテクスチャの削除をする
            // メモリリークしたっていう人がいれば優先で対応
            if (Refs.ContainsKey(preset_name))
            {
                Refs.Remove(preset_name);
            }

            if (PresetErrorMap.ContainsKey(preset_name)) {
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
                        else
                        {
                            error_message.Add("The preset JSON definition is incorrect." + preset_name);
                        }
                    }
                    catch (Exception)
                    {
                        //error_message.Add("The preset JSON definition is incorrect." + preset_name + " [wt?]: " + e.Message);
                    }
                }
                //Log.Message($"[PortraitsEx] Result ==> Target preset: {preset_name} Refs Count: {r.txs.Count} Group Filter Count: {r.group_filter.Count} PriorityWeight Count: {r.priority_weights.Count}");
                if (!Refs.ContainsKey(preset_name))
                {
                    Refs.Add(preset_name, r);
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
        private static void ReadDirectory(DirectoryInfo directory)
        {
            Log.Message($"[PortraitsEx] Target directory: {PresetDirectory.FullName}");
            System.IO.FileInfo[] files = PresetDirectory.GetFiles("*.json", System.IO.SearchOption.TopDirectoryOnly);

            foreach (FileInfo file in files)
            {
                JObject root = JObject.Parse(File.ReadAllText(@file.FullName));
                string preset_name = root["preset_name"].ToString();
                Refs r = new Refs();
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
                        else if(key == "interrupt")
                        {
                            Interrupt(preset_name, key, value, r);
                        }
                        else
                        {
                            Log.Warning("The preset JSON definition is incorrect." + preset_name);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning("The preset JSON definition is incorrect." + preset_name + " [wt?]: " + e.Message);
                    }
                }
                Log.Message($"[PortraitsEx] Result ==> Target preset: {preset_name} Refs Count: {r.txs.Count} Group Filter Count: {r.group_filter.Count} PriorityWeight Count: {r.priority_weights.Count}");
                if (!Refs.ContainsKey(preset_name))
                {
                    Refs.Add(preset_name, r);
                }
                else
                {
                    Log.Warning($"[PortraitsEx] Duplicate preset name detected. ==> Target preset: {preset_name}");
                }
            }
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
                        var tx = Textures(preset_name, cont, prop_n.Value, r);
                        r.txs.Add(Refs_key, tx);
                        if (Utility.IsRegexPattern(Refs_key))
                        {
                            r.txs_regex_cache.Add(Refs_key, new Regex(Refs_key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                        }
                    }

                }

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
                        InteractionSelectionMap.intf_regex_cache.Add(intf_key, new Regex(intf_key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                    }
                    Log.Message($"[PortraitsEx] InteractionFilter ==> Target preset: {preset_name} Key: {intf_key} matched_initiator_key: {intf.matched_initiator_key} matched_recipient_key: {intf.matched_recipient_key}");
                }
            }catch(Exception e)
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
                foreach (var token in n)
                {
                    var prop = (JProperty)token;

                    string g_k = prop.Name;

                    JToken value = prop.Value;

                    foreach (var v in (JArray)prop.Value)
                    {
                        var Refs_key = v.ToString();
                        if (!r.group_filter.ContainsKey(Refs_key))
                        {
                            r.group_filter.Add(Refs_key, g_k);
                            if (Utility.IsRegexPattern(Refs_key))
                            {
                                r.g_regex_cache.Add(Refs_key, new Regex(Refs_key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                            }

                            Log.Message($"[PortraitsEx] Group ==> Target preset: {preset_name} Group Key ==> {g_k} Value ==> {Refs_key}");
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
                        if (Utility.IsRegexPattern(Refs_key))
                        {
                            r.pw_regex_cache.Add(Refs_key, new Regex(Refs_key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                        }
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
                    else if(Refs_key == "monitor_behaviors")
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
                                    interr.group_filter.Add(iRefs_key, g_k);

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


        private static Textures Textures(string preset_name, string k, JToken n, Refs r)
        {
            //Log.Message($"[PortraitsEx] Textures ==> Target preset: {preset_name}");

            Textures tx = new Textures();
            try
            {
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
            tex.Apply();

            return tex;
        }


    }
}
