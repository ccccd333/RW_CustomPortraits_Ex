using Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs;
using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static RimWorld.ColonistBar;
using static RimWorld.PsychicRitualRoleDef;

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

        private void WriteJsonAll()
        {
            string preset_directory = PortraitCacheEx.PresetDirectory.FullName;
            string file_name = dbt_copy.selected_preset_name + ".json";
            string file_path = Path.Combine(preset_directory, file_name);

            // 先祖は１つまでバックアップする
            string backup_dir = Path.Combine(preset_directory, DateTime.Now.ToString("backup"));
            string backup_dir_ymd = Path.Combine(backup_dir, DateTime.Now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(backup_dir);
            string backup_path = Path.Combine(backup_dir, file_name + "." + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            File.Copy(file_path, backup_path, overwrite: true);

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
                // 先祖は１つまでバックアップする
                string backup_dir = Path.Combine(preset_directory, DateTime.Now.ToString("backup"));
                string backup_dir_ymd = Path.Combine(backup_dir, DateTime.Now.ToString("yyyyMMdd"));
                Directory.CreateDirectory(backup_dir);
                string backup_path = Path.Combine(backup_dir, file_name + "." + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                File.Copy(file_path, backup_path, overwrite: true);

                string json = File.ReadAllText(file_path);
                data = JObject.Parse(json);
            }
            else
            {
                data = new JObject();
            }

            data["preset_name"] = "InteractionFilter";
            JObject work_conditions;
            if (data.TryGetValue("conditions", out JToken conditions_copy))
            {
                if (conditions_copy is JObject conditions)
                {
                    work_conditions = conditions;
                }
                else
                {
                    work_conditions = new JObject();
                }
            }
            else
            {
                work_conditions = new JObject();
            }
            data["conditions"] = work_conditions;

            JObject work_interaction_filter;
            if (work_conditions.TryGetValue("interaction_filter", out JToken interaction_filter_copy))
            {
                if (interaction_filter_copy is JObject interaction_filter)
                {
                    work_interaction_filter = interaction_filter;
                }
                else
                {
                    work_interaction_filter = new JObject();
                }
            }
            else
            {
                work_interaction_filter = new JObject();
            }

            work_conditions["interaction_filter"] = work_interaction_filter;

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

            // 先祖は１つまでバックアップする
            string backup_dir = Path.Combine(preset_directory, DateTime.Now.ToString("yyyyMMdd"));
            Directory.CreateDirectory(backup_dir);
            string backup_path = Path.Combine(backup_dir, file_name);
            File.Copy(file_path, backup_path, overwrite: true);

            string json = File.ReadAllText(file_path);
            JObject data = JObject.Parse(json);
            BuildPriorityWeightNode((JObject)data["conditions"]["priority_weights"]);

            string output_json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(file_path, output_json);
        }

        private void BuildFullJson(JObject root)
        {
            // preset_name
            root["preset_name"] = dbt_copy.selected_preset_name.Trim();

            // conditions
            JObject work_conditions;
            if (root.TryGetValue("conditions", out JToken value))
            {
                if (value is JObject conditions)
                {
                    work_conditions = conditions;
                }
                else
                {
                    work_conditions = new JObject();
                }
            }
            else
            {
                work_conditions = new JObject();
            }
            BuildConditionsNode(work_conditions);
            root["conditions"] = work_conditions;
        }


        private void BuildConditionsNode(JObject conditions)
        {
            // fallback_mood
            conditions["fallback_mood"] = "Idle";
            conditions["fallback_mood_on_death"] = "Dead";
            // refs
            JObject work_refs;
            if (conditions.TryGetValue("refs", out JToken refs_copy))
            {
                if (refs_copy is JObject refs)
                {
                    work_refs = refs;
                }
                else
                {
                    work_refs = new JObject();
                }
            }
            else
            {
                work_refs = new JObject();
            }
            BuildRefsNode(work_refs);
            conditions["refs"] = work_refs;

            // group
            JObject work_group;
            if (conditions.TryGetValue("group", out JToken group_copy))
            {
                if (group_copy is JObject group)
                {
                    work_group = group;
                }
                else
                {
                    work_group = new JObject();
                }
            }
            else
            {
                work_group = new JObject();
            }
            BuildGroupNode(work_group);
            conditions["group"] = work_group;

            // priority_weight
            // priority_weightのみ優先順位を全ての項目に対して並び変えるので、新規でも既存でも上書きする

            JObject work_priority_weight = new JObject(); ;
            //if (conditions.TryGetValue("priority_weight", out JToken priority_weight_copy))
            //{
            //    if (priority_weight_copy is JObject priority_weight)
            //    {
            //        work_priority_weight = priority_weight;
            //    }
            //    else
            //    {
            //        work_priority_weight = new JObject();
            //    }
            //}
            //else
            //{
            //    work_priority_weight = new JObject();
            //}
            BuildPriorityWeightNode(work_priority_weight);
            conditions["priority_weights"] = work_priority_weight;

        }

        private void BuildRefsNode(JObject refs)
        {
            // グループ名のものがすでに存在する場合は上書き
            refs[gp_copy.result_edit_target_group_name] = new JObject()
            {
                ["textures"] = new JObject
                {
                    ["animation_mode"] = pge_copy.result_texture_meta.IsAnimation ? 1 : 0,
                    ["display_duration"] = pge_copy.result_texture_meta.display_duration,
                    ["files"] = new JArray {
                        pge_copy.result_texture_meta.file_path }
                }
            };

            refs["Idle"] = new JObject()
            {
                ["textures"] = new JObject
                {
                    ["animation_mode"] = pge_copy.result_texture_idle_meta.IsAnimation ? 1 : 0,
                    ["display_duration"] = pge_copy.result_texture_idle_meta.display_duration,
                    ["files"] = new JArray {
                        pge_copy.result_texture_idle_meta.file_path }
                }
            };

            refs["Dead"] = new JObject()
            {
                ["textures"] = new JObject
                {
                    ["animation_mode"] = pge_copy.result_texture_dead_meta.IsAnimation ? 1 : 0,
                    ["display_duration"] = pge_copy.result_texture_dead_meta.display_duration,
                    ["files"] = new JArray {
                        pge_copy.result_texture_dead_meta.file_path }
                }
            };
        }

        private void BuildGroupNode(JObject group)
        {
            group[gp_copy.result_edit_target_group_name] = new JArray(gp_copy.result_target_group_rows);
        }

        private void BuildPriorityWeightNode(JObject priority_weight)
        {
            foreach(var weight_name in pwe_copy.result_priority_weight_order)
            {
                var weight = pwe_copy.result_priority_weights[weight_name];

                priority_weight[weight_name] = new JObject()
                {
                    ["category"] = 0,
                    ["weight"] = weight.weight
                };
            }
        }
    }
}
