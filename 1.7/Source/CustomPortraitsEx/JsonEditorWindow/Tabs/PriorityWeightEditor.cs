using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class PriorityWeightEditor : TabBase
    {
        int stage = 0;
        Refs refs;
        Dictionary<string, string> buf_st = new Dictionary<string, string>();
        private List<string> temp_priority_weight_order = new List<string>();
        
        Dictionary<string, PriorityWeights> temp_priority_weights = new Dictionary<string, PriorityWeights>();

        // 上からの優先順位(グループ名)を持っている
        public List<string> result_priority_weight_order = new List<string>();
        // グループ名に対する確率などディクショナリ
        public Dictionary<string, PriorityWeights> result_priority_weights = new Dictionary<string, PriorityWeights>();
        public void Draw(Rect inRect, string edit_target_group_name, string selected_preset_name)
        {
            Listing_Standard listing = Begin(inRect);
            listing.Label(Helper.Label("RCP_PW_Desc1"));
            listing.Label(Helper.Label("RCP_PW_Desc2"));
            listing.Label(Helper.Label("RCP_PW_Desc3"));
            listing.Label(Helper.Label("RCP_PW_Desc4"));
            listing.GapLine();

            if(selected_preset_name == "")
            {
                listing.Label(Helper.Label("RCP_PW_Desc5"));
             
            }
            else if (!PortraitCacheEx.Refs.ContainsKey(selected_preset_name))
            {
                if (temp_priority_weight_order.Count == 0)
                {
                    stage = 1;
                    temp_priority_weight_order.Add(edit_target_group_name);
                    temp_priority_weights.Add(edit_target_group_name, new PriorityWeights());
                    buf_st.Add(edit_target_group_name, "100");
                }

                switch (stage)
                {
                    case 1:
                        DrawPriorityWeights(listing);
                        break;
                    case 2:
                        EndEditing(listing);
                        break;
                        
                }
            }
            else
            {

                refs = PortraitCacheEx.Refs[selected_preset_name];

                if (temp_priority_weight_order.Count == 0)
                {
                    temp_priority_weight_order = refs.priority_weight_order.ToList();
                    temp_priority_weights = refs.priority_weights.ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value.Clone()
                        );

                    foreach (var key in temp_priority_weight_order)
                    {
                        if (temp_priority_weights.TryGetValue(key, out var pw))
                        {
                            buf_st[key] = pw.weight.ToString();
                        }
                    }
                }

                if (edit_target_group_name != "" && !temp_priority_weight_order.Contains(edit_target_group_name))
                {
                    temp_priority_weight_order.Add(edit_target_group_name);
                    temp_priority_weights.Add(edit_target_group_name, new PriorityWeights());
                    buf_st.Add(edit_target_group_name, "100");
                    if (stage >= 1)
                    {
                        stage = 0;
                    }
                }

                switch (stage)
                {
                    case 0:
                        ShiftItem(listing);
                        break;
                    case 1:
                        DrawPriorityWeights(listing);
                        break;
                    case 2:
                        EndEditing(listing);
                        break;
                }
            }
            SetStage();

            End(listing);
        }

        public void Reset()
        {
            stage = 0;
            call_id = "";
            refs = null;
            buf_st.Clear();
            temp_priority_weight_order.Clear();
            temp_priority_weights.Clear();
            result_priority_weight_order.Clear();
            result_priority_weights.Clear();
        }

        private void ShiftItem(Listing_Standard listing)
        {
            listing.Label(Helper.Label("RCP_PW_ShiftItemDesc1"));
            listing.GapLine();

            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "back";
            }

            listing.GapLine();

            for (int i = 0; i < temp_priority_weight_order.Count; i++)
            {
                Rect row = listing.GetRect(30f);
                Widgets.Label(row.LeftPart(0.7f), temp_priority_weight_order[i]);

                // 上ボタン
                if (i > 0)
                {
                    Rect upRect = new Rect(row.xMax - 50f, row.y, 20f, 30f);
                    if (Widgets.ButtonText(upRect, "▲"))
                    {
                        var tmp = temp_priority_weight_order[i];
                        temp_priority_weight_order[i] = temp_priority_weight_order[i - 1];
                        temp_priority_weight_order[i - 1] = tmp;
                    }
                }

                // 下ボタン
                if (i < temp_priority_weight_order.Count - 1)
                {
                    Rect downRect = new Rect(row.xMax - 25f, row.y, 20f, 30f);
                    if (Widgets.ButtonText(downRect, "▼"))
                    {
                        var tmp = temp_priority_weight_order[i];
                        temp_priority_weight_order[i] = temp_priority_weight_order[i + 1];
                        temp_priority_weight_order[i + 1] = tmp;
                    }
                }
            }

            listing.GapLine();

            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                call_id = "order end";
            }
        }

        private void DrawPriorityWeights(Listing_Standard listing)
        {
            listing.Label(Helper.Label("RCP_PW_DrawPriorityWeightsDesc1"));
            listing.GapLine();

            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "order end->back";
            }

            listing.GapLine();

            for (int i = 0; i < temp_priority_weight_order.Count; i++)
            {
                string key = temp_priority_weight_order[i];

                Rect row = listing.GetRect(30f);

                Widgets.Label(row.LeftPart(0.6f), key);

                Rect inputRect = new Rect(row.xMax - 60f, row.y, 60f, 30f);
                PriorityWeights value = temp_priority_weights[key];
                int buf_value = value.weight;
                string buf_input = "0";

                if (buf_st.TryGetValue(key, out var bfi))
                {
                    buf_input = bfi;
                }

                Widgets.TextFieldNumeric(inputRect, ref buf_value, ref buf_input, 0, 100);
                value.weight = buf_value;
                buf_st[key] = buf_input;
            }

            listing.GapLine();

            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                call_id = "order end->weight end";

                result_priority_weight_order = new List<string>(temp_priority_weight_order);
                result_priority_weights = temp_priority_weights.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
            }
        }

        private void EndEditing(Listing_Standard listing)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "order end->weight end->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_PW_EndEditingDesc1"));
        }


        private void SetStage()
        {
            if (stage == 0)
            {
                if (call_id == "order end")
                {
                    stage = 1;
                }
                else if (call_id == "back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 1)
            {
                if (call_id == "order end->weight end")
                {
                    stage = 2;
                }
                else if (call_id == "order end->back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 2)
            {
                if (call_id == "order end->weight end->back")
                {
                    Reset();
                    stage = 0;
                }
            }
        }


    }
}
