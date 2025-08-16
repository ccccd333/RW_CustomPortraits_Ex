using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public static void Update()
        {
            Log.Message($"[PortraitsEx] Updating cache from directory: {Directory.FullName}");
            if (!Directory.Exists) Directory.Create();

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

        private static void ReadDirectory(DirectoryInfo directory)
        {
            Log.Message($"[PortraitsEx] Target directory: {PresetDirectory.FullName}");
            System.IO.FileInfo[] files = PresetDirectory.GetFiles("*.json", System.IO.SearchOption.TopDirectoryOnly);

            foreach (FileInfo file in files)
            {
                JObject root = JObject.Parse(File.ReadAllText(@file.FullName));
                string preset_name = root["preset_name"].ToString();
                Refs r = new Refs();

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
                        else
                        {
                            throw new Exception("The preset JSON definition is incorrect." + preset_name);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("The preset JSON definition is incorrect." + preset_name + " [wt?]: " + e.Message);
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
                InteractionSelectionMap.InteractionFilter.Add(intf_key, intf);
                if (Utility.IsRegexPattern(intf_key))
                {
                    Log.Message($"[PortraitsEx] InteractionFilter ==> Target preset: {preset_name} ADDREGEX");
                    InteractionSelectionMap.intf_regex_cache.Add(intf_key, new Regex(intf_key, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                }
                Log.Message($"[PortraitsEx] InteractionFilter ==> Target preset: {preset_name} Key: {intf_key} matched_initiator_key: {intf.matched_initiator_key} matched_recipient_key: {intf.matched_recipient_key}");
            }
        }
        private static void Group(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] Group ==> Target preset: {preset_name}");

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

        private static void PriorityWeights(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] PriorityWeights ==> Target preset: {preset_name}");

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

        private static Textures Textures(string preset_name, string k, JToken n, Refs r)
        {
            Log.Message($"[PortraitsEx] Textures ==> Target preset: {preset_name}");

            Textures tx = new Textures();

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

                            for (; range_from <= range_to; range_from++)
                            {
                                string f = Directory.FullName + "/" + base_path + range_from.ToString() + d;

                                //Log.Message($"[PortraitsEx] Load Protraits: {f}");
                                byte[] data = File.ReadAllBytes(f);
                                Texture2D tex = LoadTextureDDS(data);
                                tx.txs.Add(tex);
                            }
                        }
                        else
                        {
                            string d = "";
                            Utility.Delimiter(portrait_path, out d);

                            if (d.ToLower() == ".dds")
                            {
                                string f = Directory.FullName + "/" + v;
                                byte[] data = File.ReadAllBytes(f);
                                Texture2D tex = LoadTextureDDS(data);
                                tx.txs.Add(tex);
                            }
                            else
                            {
                                string f = Directory.FullName + "/" + v;
                                byte[] data = File.ReadAllBytes(f);
                                Texture2D tex = new Texture2D(2, 2);
                                tex.LoadImage(data);
                                tx.txs.Add(tex);
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("The preset JSON definition is incorrect." + preset_name + "." + k);
                }
            }

            return tx;
        }

        private static Texture2D LoadTextureDDS(byte[] data)
        {
            DDS dds = DDS.Parse(data);

            TextureFormat fmt;
            if (dds.ddpfPixelFormat.IsDXT1)
            {
                fmt = TextureFormat.DXT1;
            }
            else if (dds.ddpfPixelFormat.IsDXT5)
            {
                fmt = TextureFormat.DXT5;
                if (dds.dwWidth % 4 != 0 || dds.dwHeight % 4 != 0)
                {
                    throw new FormatException($"DXT5 format requires dimensions to be divisable by 4: {dds.dwWidth}x{dds.dwHeight}");
                }
            }
            else
            {
                // Not sure why I'm so pedant about exact wrong format, but I woke up after already writing this
                if (dds.ddpfPixelFormat.dwFlags.HasFlag(DDPF.FourCC))
                {
                    throw new FormatException($"Unsupported pixel format: {dds.ddpfPixelFormat.StringFourCC}");
                }
                else if (dds.ddpfPixelFormat.dwFlags.HasFlag(DDPF.RGB))
                {
                    // Thankfully I stopped myself before reporting in the exact A_R_G_B_ notation
                    if (dds.ddpfPixelFormat.dwFlags.HasFlag(DDPF.AlphaPixels))
                        throw new FormatException($"Unsupported pixel format: ARGB");
                    else throw new FormatException($"Unsupported pixel format: RGB");
                }
                else
                {
                    throw new FormatException($"Unsupported pixel format: unknown");
                }
            }

            // DDSD_LINEARSIZE is required for compressed formats and DXTn are all compressed
            if (!dds.dwFlags.HasFlag(DDSD.LinearSize))
                throw new FormatException($"Linear size flag not set for a compressed format (0x{(uint)dds.dwFlags:X8})");
            if (dds.dwFlags.HasFlag(DDSD.Pitch))
                throw new FormatException($"Pitch flag set for a compressed format (0x{(uint)dds.dwFlags:X8})");

            // Pixel data size should be equal to dwPitchOrLinearSize since DDSD_LINEARSIZE is required for DXT, but I can't bring myself to trust it.
            byte[] dxt = new byte[data.Length - dds.DataOffset];
            Buffer.BlockCopy(data, (int)dds.DataOffset, dxt, 0, data.Length - (int)dds.DataOffset);

            // Maybe this is important, I dunno.
            int mipMapCount = dds.dwFlags.HasFlag(DDSD.MipmapCount) && dds.dwMipMapCount > 1 ? (int)dds.dwMipMapCount : 1;

            Texture2D tex = new Texture2D((int)dds.dwWidth, (int)dds.dwHeight, fmt, mipMapCount, false);

            if (dds.ddpfPixelFormat.IsDXT1)
                Utility.FlipDXT1(dxt, (int)dds.dwWidth, (int)dds.dwHeight);
            else
                Utility.FlipDXT5(dxt, (int)dds.dwWidth, (int)dds.dwHeight);
            tex.LoadRawTextureData(dxt);
            tex.Apply();

            return tex;
        }
    }
}
