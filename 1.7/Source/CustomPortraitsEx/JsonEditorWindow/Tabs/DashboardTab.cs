using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class DashboardTab : TabBase
    {

        public static string WRITE_JSON_ALL = "write json all";
        public static string WRITE_NEW_JSON_ALL = "write new json all";
        public static string WRITE_JSON_PW = "write json pw";
        public static string WRITE_JSON_INTERACTIONS = "write json interactions";

        public string selected_preset_name = "";
        int stage = 0;
        string new_json_name = "";
        bool new_json_edit = false;

        public List<string> json_write_modes = new List<string>();
        public bool change_preset = false;

        public void Draw(Rect inRect, Dictionary<string, bool> end_flags)
        {
            Listing_Standard listing = Begin(inRect);

            switch (stage)
            {
                case 0:
                    DrawEditorContent(listing, end_flags);
                    break;
                case 1:
                    DrawPresetNameInput(listing);
                    break;
            }

            SetStage();

            End(listing);
        }

        public void Reset()
        {
            stage = 0;
            call_id = "";
            selected_preset_name = "";
            new_json_name = "";
            new_json_edit = false;
            json_write_modes.Clear();
            change_preset = false;
        }

        private void DrawEditorContent(Listing_Standard listing, Dictionary<string, bool> end_flags)
        {

            listing.Label(Helper.Label("RCP_DBR_SelectPreset"));

            List<string> presets = new List<string>(PortraitCacheEx.Refs.Keys);
            listing.Label(Helper.Label("RCP_DBR_SelectAnItem"));
            listing.GapLine();

            Rect clear_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(clear_rect.RightPart(0.55f), Helper.Label("RCP_B_InputClear")))
            {
                call_id = "clear";
            }

            listing.GapLine();

            if (listing.ButtonText(Helper.Label("RCP_B_PresetCreate")))
            {
                call_id = "json new";
            }

            listing.GapLine();

            // リスト表示
            foreach (var item in presets)
            {
                if (item == "InteractionFilter") continue;

                if (listing.ButtonText(item))
                {
                    // クリックされたら TextField にコピー
                    if(selected_preset_name != item)
                    {
                        change_preset = true;
                    }
                    selected_preset_name = item;
                }
            }

            listing.GapLine();

            Rect edit_selected_json_rect = listing.GetRect(30f);
            Widgets.Label(edit_selected_json_rect.LeftPart(0.6f), Helper.Label("RCP_DBR_SelectFrom"));
            Widgets.Label(edit_selected_json_rect.RightPart(0.15f), selected_preset_name);

            listing.GapLine();
            
            listing.Label(Helper.Label("RCP_DBR_Validate"));

            listing.GapLine();

            foreach (var item in end_flags)
            {
                bool marked_for_tabs = item.Value;

                Rect row_rect = listing.GetRect(30f);

                // ラベル
                Widgets.Label(row_rect.LeftPart(0.6f), $"「{item.Key}」");
                Rect checkbox_rect = row_rect.RightPart(0.15f);
                Widgets.Checkbox(checkbox_rect.x, checkbox_rect.y, ref marked_for_tabs);

            }

            listing.GapLine();

            bool marked_for = true;
            bool marked_for_priority_weight = true;
            bool marked_for_interaction_filter = true;

            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_WriteJson")))
            {

                foreach (var item in end_flags)
                {
                    if(item.Key != "PriorityWeightEditor" && item.Value)
                    {
                        marked_for_priority_weight = false;
                    }
                    else if (item.Key == "PriorityWeightEditor" && !item.Value)
                    {
                        marked_for_priority_weight = false;
                    }

                    if(item.Key == "InteractionFilterEditor" && !item.Value)
                    {
                        marked_for_interaction_filter = false;
                    }

                    if (!item.Value)
                    {
                        if (item.Key != "InteractionFilterEditor")
                        {
                            marked_for = false;
                        }
                    }
                }

                if (marked_for)
                {
                    if (new_json_edit)
                    {
                        json_write_modes.Add(WRITE_NEW_JSON_ALL);
                    }
                    else
                    {
                        json_write_modes.Add(WRITE_JSON_ALL);
                    }
                }

                if (marked_for_interaction_filter)
                {
                    json_write_modes.Add(WRITE_JSON_INTERACTIONS);
                }

                if (!new_json_edit && marked_for_priority_weight)
                {
                    json_write_modes.Add(WRITE_JSON_PW);
                }                
            }

            listing.GapLine();

            if(!marked_for || !marked_for_interaction_filter || !marked_for_priority_weight)
            {
                if (!marked_for)
                {
                    listing.Label(Helper.Label("RCP_DBRE_Error1"));
                }

                if (!marked_for_interaction_filter)
                {
                    listing.Label(Helper.Label("RCP_DBRE_Error2"));
                }

                if (!marked_for_priority_weight)
                {
                    listing.Label(Helper.Label("RCP_DBRE_Error3"));
                }
            }
        }

        void DrawPresetNameInput(Listing_Standard listing)
        {
            listing.Label(Helper.Label("RCP_DBR_InputPresetName"));
            listing.Label(Helper.Label("RCP_DBR_InputPresetNameLable"));
            listing.GapLine();

            if (new_json_name != "" && PortraitCacheEx.Refs.ContainsKey(new_json_name))
            {
                listing.Label(Helper.Label("RCP_DBR_InputSamePresetName"));
            }

            listing.GapLine();

            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "json new->back";
            }

            listing.GapLine();

            Rect row = listing.GetRect(30f);

            Widgets.Label(row.LeftPart(0.6f), Helper.Label("RCP_DBR_EnterPresetName"));

            Rect text_rect = new Rect(row.x + row.width * 0.6f, row.y, row.width * 0.25f, row.height);
            new_json_name = Widgets.TextField(text_rect, new_json_name);

            Rect button_rect = new Rect(row.x + row.width * 0.87f, row.y, row.width * 0.13f, row.height);
            if (Widgets.ButtonText(button_rect, Helper.Label("RCP_B_Enter")))
            {
                new_json_name = new_json_name.Trim();

                if (!string.IsNullOrEmpty(new_json_name))
                {
                    call_id = "json new->end";
                    if(new_json_name != selected_preset_name)
                    {
                        change_preset = true;
                    }
                    selected_preset_name = new_json_name;
                    new_json_edit = true;
                }
            }
        }

        private void SetStage()
        {
            if (stage == 0)
            {
                if (call_id == "json new")
                {
                    stage = 1;
                }
                else if (call_id == "clear")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 1)
            {
                if (call_id == "json new->end")
                {
                    stage = 0;
                }
                else if (call_id == "json new->back")
                {
                    Reset();
                    stage = 0;
                }
            }
        }
    }
}
