using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class GroupEditor : TabBase
    {
        int stage = 0;
        private string edit_target_group_name = "";
        Refs refs;
        private Dictionary<string, List<string>> temp_group_filter = new Dictionary<string, List<string>>();
        private List<string> temp_target_group_rows = new List<string>();
        private List<string> temp_remove_group_rows = new List<string>();

        // グループのキー側
        public string result_edit_target_group_name = "";
        // グループの値側
        public List<string> result_target_group_rows = new List<string>();

        public void Draw(Rect inRect, List<string> selected_thoughts, List<string> selected_Interactions, List<string> result_interaction_filter, string selected_preset_name)
        {
            Listing_Standard listing = Begin(inRect);

            List<string> merged = new List<string>(selected_thoughts);
            merged.AddRange(result_interaction_filter);
            
            listing.Label(Helper.Label("RCP_GE_Desc1"));
            listing.Label(Helper.Label("RCP_GE_Desc2"));
            listing.Label(Helper.Label("RCP_GE_Desc3"));
            listing.Label(Helper.Label("RCP_GE_Desc4"));
            
            listing.GapLine();

            if (merged.Count <= 0)
            {
                listing.Label(Helper.Label("RCP_GE_Desc5"));
            }else if(selected_Interactions.Count > 0 && result_interaction_filter.Count == 0)
            {
                listing.Label(Helper.Label("RCP_GE_Desc6"));
                listing.Label(Helper.Label("RCP_GE_Desc7"));
            }
            else if (selected_preset_name == "")
            {
                listing.Label(Helper.Label("RCP_GE_Desc8"));
            }
            else
            {
                switch (stage)
                {
                    case 0:
                        CreateOrEditGroup(listing, selected_preset_name);
                        break;
                    case 1:
                        EnterGroupName(listing);
                        break;
                    case 2:
                        EditGroup(listing, merged);
                        break;
                    case 3:
                        EndEditing(listing);
                        break;
                }

                SetStage();
            }

            End(listing);
        }

        public void Reset()
        {
            stage = 0;
            call_id = "";
            edit_target_group_name = "";
            result_edit_target_group_name = "";
            refs = null;
            temp_group_filter.Clear();
            temp_target_group_rows.Clear();
            temp_remove_group_rows.Clear();
            result_target_group_rows.Clear();
        }

        private void CreateOrEditGroup(Listing_Standard listing, string selected_preset_name)
        {
            listing.Label(Helper.Label("RCP_GE_CreateOrEditGroupDesc1"));
            if (!PortraitCacheEx.Refs.ContainsKey(selected_preset_name))
            {
                if (listing.ButtonText(Helper.Label("RCP_B_Create")))
                {
                    call_id = "create";
                }
            }
            else
            {
                if (listing.ButtonText(Helper.Label("RCP_B_Create")))
                {
                    call_id = "create";
                }

                refs = PortraitCacheEx.Refs[selected_preset_name];
                temp_group_filter.Clear();
                foreach (var kv in refs.group_filter)
                {
                    if (!temp_group_filter.ContainsKey(kv.Value))
                    {
                        temp_group_filter[kv.Value] = new List<string>();
                    }
                    temp_group_filter[kv.Value].Add(kv.Key);
                }

                listing.Label(Helper.Label("RCP_GE_CreateOrEditGroupDesc2"));
                foreach (var gf in temp_group_filter)
                {

                    if (listing.ButtonText(gf.Key))
                    {
                        call_id = "edit";
                        edit_target_group_name = gf.Key;
                        temp_target_group_rows = temp_group_filter[edit_target_group_name];
                    }
                }
            }
        }

        private void EnterGroupName(Listing_Standard listing)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "create->back";
            }

            listing.GapLine();

            Rect enter_rect = listing.GetRect(30f);
            Widgets.Label(enter_rect.LeftPart(0.6f), Helper.Label("RCP_GE_EnterGroupNameDesc1"));

            listing.GapLine();

            Rect input_rect = listing.GetRect(30f);
            edit_target_group_name = Widgets.TextField(input_rect, edit_target_group_name);

            listing.GapLine();

            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                call_id = "create->enter name";
            }

        }

        private void EditGroup(Listing_Standard listing, List<string> merged)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_GE_EditGroupDesc1"));
            listing.Label(Helper.Label("RCP_GE_EditGroupDesc2"));
            Rect selected_rect = listing.GetRect(30f);
            Widgets.Label(selected_rect.LeftPart(0.6f), Helper.Label("RCP_GE_EditGroupDesc3"));
            Widgets.Label(selected_rect.RightPart(0.15f), edit_target_group_name);

            listing.Label(Helper.Label("RCP_GE_EditGroupDesc4"));
            for (int i = temp_target_group_rows.Count - 1; i >= 0; i--)
            {
                var row = temp_target_group_rows[i];
                Rect row_rect = listing.GetRect(30f);
                Widgets.Label(row_rect.LeftPart(0.6f), "     ==>" + row);

                if (Widgets.ButtonText(row_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Remove")))
                {
                    temp_target_group_rows.RemoveAt(i);
                    if (!temp_remove_group_rows.Contains(row))
                    {
                        temp_remove_group_rows.Add(row);
                    }
                }
            }

            listing.GapLine();

            Rect push_rect = listing.GetRect(30f);
            Widgets.Label(push_rect.LeftPart(0.6f), Helper.Label("RCP_GE_EditGroupDesc5"));
            if(Widgets.ButtonText(push_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_AddAll")))
            {
                foreach (var row in merged)
                {

                    if (!temp_target_group_rows.Contains(row))
                    {
                        temp_target_group_rows.Add(row);
                    }
                }
            }

            listing.Gap();

            foreach (var row in merged)
            {
                

                Rect row_rect = listing.GetRect(30f);
                // ラベル
                Widgets.Label(row_rect.LeftPart(0.6f), "     ==>" + row);

                // SELECT ボタン
                if (Widgets.ButtonText(row_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Add")))
                {
                    if (!temp_target_group_rows.Contains(row))
                    {
                        temp_target_group_rows.Add(row);
                    }
                }
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_GE_EditGroupDesc6"));
            foreach (var row in temp_remove_group_rows)
            {

                Rect row_rect = listing.GetRect(30f);
                // ラベル
                Widgets.Label(row_rect.LeftPart(0.6f), "     ==>" + row);

                // SELECT ボタン
                if (Widgets.ButtonText(row_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Add")))
                {
                    if (!temp_target_group_rows.Contains(row))
                    {
                        temp_target_group_rows.Add(row);
                    }
                }
            }
            listing.GapLine();
            Rect enter_rect = listing.GetRect(30f);
            
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                if (temp_target_group_rows.Count > 0)
                {
                    result_edit_target_group_name = edit_target_group_name;
                    result_target_group_rows = new List<string>(temp_target_group_rows);
                    call_id = "edit->end editing";
                }
            }

        }

        private void EndEditing(Listing_Standard listing)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit->end editing->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_GE_EndEditingDesc1"));
            listing.Label(Helper.Label("RCP_GE_EndEditingDesc2"));

            listing.GapLine();
            listing.Label($"{Helper.Label("RCP_GE_EndEditingDesc3")}{edit_target_group_name}");
            listing.Label(Helper.Label("RCP_GE_EndEditingDesc4"));
            foreach (var kv in temp_target_group_rows)
            {
                listing.Label($"    ==>{kv}");
            }

        }


        private void SetStage()
        {
            if(stage == 0)
            {
                if(call_id == "create")
                {
                    stage = 1;
                }
                else if (call_id == "edit")
                {
                    stage = 2;
                }
            }
            else if(stage == 1)
            {
                if(call_id == "create->enter name")
                {
                    stage = 2;
                    call_id = "edit";
                }
                else if(call_id == "create->back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if(stage == 2)
            {
                if(call_id == "edit->back")
                {
                    Reset();
                    stage = 0;
                }
                else if(call_id == "edit->end editing")
                {
                    stage = 3;
                }
            }else if(stage == 3)
            {
                if (call_id == "edit->end editing->back")
                {
                    Reset();
                    stage = 0;
                }
            }
        }


    }
}
