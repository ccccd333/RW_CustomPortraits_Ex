using Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow
{
    public class CustomPortraitJsonWriter
    {
        DashboardTab dbt_copy;
        InteractionFilterEditor ife_copy;
        GroupEditor gp_copy;
        PortraitGroupEditor pge_copy;
        PriorityWeightEditor pwe_copy;

        public void Write(
            DashboardTab dbt,
            InteractionFilterEditor ife,
            GroupEditor gp,
            PortraitGroupEditor pge,
            PriorityWeightEditor pwe)
        {
            dbt_copy = dbt;
            ife_copy = ife;
            gp_copy = gp;
            pge_copy = pge;
            pwe_copy = pwe;

            foreach (var mode in dbt.json_write_modes)
            {
                if (mode == DashboardTab.WRITE_JSON_ALL)
                {
                    WriteJsonAll();
                }
                else if (mode == DashboardTab.WRITE_NEW_JSON_ALL)
                {
                    WriteNewJsonAll();
                }
                else if (mode == DashboardTab.WRITE_JSON_INTERACTIONS)
                {
                    WriteJsonInteractions();
                }
                else if (mode == DashboardTab.WRITE_JSON_PW)
                {
                    WriteJsonPriorityWeight();
                }
            }
        }

        private static void CreateBackupIfExists(string file_path, string file_name)
        {
            if (!File.Exists(file_path))
                return;

            DateTime now = DateTime.Now;
            string backup_dir = Path.Combine(PortraitCacheEx.Directory.FullName, "Backups", now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(backup_dir);
            string backup_path = Path.Combine(backup_dir, $"{file_name}.{now:yyyyMMdd_HHmmss}");
            File.Copy(file_path, backup_path, overwrite: true);
        }

        private void WriteJsonAll()
        {
            string preset_directory = PortraitCacheEx.PresetDirectory.FullName;
            string file_name = dbt_copy.selected_preset_name + ".json";
            string file_path = Path.Combine(preset_directory, file_name);

            CreateBackupIfExists(file_path, file_name);

            string json = File.ReadAllText(file_path);
            JObject data = JObject.Parse(json);
            BuildFullJson(data);

            string output_json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(file_path, output_json);
        }

        private void WriteNewJsonAll()
        {
            string preset_directory = PortraitCacheEx.PresetDirectory.FullName;
            string file_name = dbt_copy.selected_preset_name + ".json";
            string file_path = Path.Combine(preset_directory, file_name);

            if (File.Exists(file_path))
            {
                WriteJsonAll();
                return;
            }
            JObject data = new JObject();
            BuildFullJson(data);

            string output_json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(file_path, output_json);
        }

        private void WriteJsonInteractions()
        {
            string preset_directory = PortraitCacheEx.PresetDirectory.FullName;
            string file_name = "InteractionFilter.json";
            string file_path = Path.Combine(preset_directory, file_name);

            JObject data;
            if (File.Exists(file_path))
            {
                CreateBackupIfExists(file_path, file_name);
                string json = File.ReadAllText(file_path);
                data = JObject.Parse(json);
            }
            else
            {
                data = new JObject();
            }

            data["preset_name"] = "InteractionFilter";
            if (!(data["conditions"] is JObject work_conditions))
            {
                work_conditions = new JObject();
                data["conditions"] = work_conditions;
            }

            if (!(work_conditions["interaction_filter"] is JObject work_interaction_filter))
            {
                work_interaction_filter = new JObject();
                work_conditions["interaction_filter"] = work_interaction_filter;
            }

            foreach (var rsi in ife_copy.result_selected_interactions)
            {
                var riv = ife_copy.result_interaction_value;
                work_interaction_filter[rsi] = new JObject()
                {
                    ["is_recipient"] = riv.is_recipient ? 1 : 0,
                    ["matched_recipient_key"] = riv.matched_recipient_key,
                    ["is_initiator"] = riv.is_initiator ? 1 : 0,
                    ["matched_initiator_key"] = riv.matched_initiator_key,
                    ["cache_duration_seconds"] = riv.cache_duration_seconds
                };
            }

            string output_json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(file_path, output_json);
        }

        private void WriteJsonPriorityWeight()
        {
            string preset_directory = PortraitCacheEx.PresetDirectory.FullName;
            string file_name = dbt_copy.selected_preset_name + ".json";
            string file_path = Path.Combine(preset_directory, file_name);

            CreateBackupIfExists(file_path, file_name);

            string json = File.ReadAllText(file_path);
            JObject data = JObject.Parse(json);
            if (!(data["conditions"] is JObject conditions))
            {
                conditions = new JObject();
                data["conditions"] = conditions;
            }

            // 既存 JObject へのインデクサ代入は元のプロパティ順を維持してしまうため、新規 JObject で置き換えて並び順を反映する
            JObject new_priority_weights = new JObject();
            BuildPriorityWeightNode(new_priority_weights);
            conditions["priority_weights"] = new_priority_weights;

            string output_json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(file_path, output_json);
        }

        private void BuildFullJson(JObject root)
        {
            root["preset_name"] = dbt_copy.selected_preset_name.Trim();

            if (!(root["conditions"] is JObject work_conditions))
            {
                work_conditions = new JObject();
            }
            BuildConditionsNode(work_conditions);
            root["conditions"] = work_conditions;
        }

        private void BuildConditionsNode(JObject conditions)
        {
            conditions["fallback_mood"] = "Idle";
            conditions["fallback_mood_on_death"] = "Dead";

            if (!(conditions["refs"] is JObject work_refs))
            {
                work_refs = new JObject();
            }
            BuildRefsNode(work_refs);
            conditions["refs"] = work_refs;

            if (!(conditions["group"] is JObject work_group))
            {
                work_group = new JObject();
            }
            BuildGroupNode(work_group);
            conditions["group"] = work_group;

            JObject work_priority_weight = new JObject();
            BuildPriorityWeightNode(work_priority_weight);
            conditions["priority_weights"] = work_priority_weight;
        }

        private void BuildRefsNode(JObject refs)
        {
            refs[gp_copy.result_edit_target_group_name] = new JObject()
            {
                ["textures"] = new JObject
                {
                    ["animation_mode"] = pge_copy.result_texture_meta.IsAnimation ? 1 : 0,
                    ["display_duration"] = pge_copy.result_texture_meta.display_duration,
                    ["files"] = new JArray { pge_copy.result_texture_meta.file_path }
                }
            };

            refs["Idle"] = new JObject()
            {
                ["textures"] = new JObject
                {
                    ["animation_mode"] = pge_copy.result_texture_idle_meta.IsAnimation ? 1 : 0,
                    ["display_duration"] = pge_copy.result_texture_idle_meta.display_duration,
                    ["files"] = new JArray { pge_copy.result_texture_idle_meta.file_path }
                }
            };

            refs["Dead"] = new JObject()
            {
                ["textures"] = new JObject
                {
                    ["animation_mode"] = pge_copy.result_texture_dead_meta.IsAnimation ? 1 : 0,
                    ["display_duration"] = pge_copy.result_texture_dead_meta.display_duration,
                    ["files"] = new JArray { pge_copy.result_texture_dead_meta.file_path }
                }
            };
        }

        private void BuildGroupNode(JObject group)
        {
            group[gp_copy.result_edit_target_group_name] = new JArray(gp_copy.result_target_group_rows);
        }

        private void BuildPriorityWeightNode(JObject priority_weight)
        {
            foreach (var weight_name in pwe_copy.result_priority_weight_order)
            {
                var weight = pwe_copy.result_priority_weights[weight_name];

                priority_weight[weight_name] = new JObject()
                {
                    ["category"] = (int)weight.category,
                    ["weight"] = weight.weight
                };
            }
        }
    }
}
