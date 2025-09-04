using Foxy.CustomPortraits.CustomPortraitsEx.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class InteractionFilterEditor : TabBase
    {
        int stage = 0;
        InteractionSelectionMap ism;

        private string edit_target_intf = "";
        private string edit_target_intf_initiator = "";
        private string edit_target_intf_recipient = "";
        private int edit_target_cache_duration_seconds = 12;
        private string filter_text = "";
        string cache_duration_seconds_input_buffer = "12";

        private Dictionary<string, List<InteractionFilter>> temp_reverse_interaction_filter = new Dictionary<string, List<InteractionFilter>>();
        //private List<InteractionFilter> temp_interaction_filter_row = new List<InteractionFilter>();
        private List<string> temp_remove_interaction_filter_rows = new List<string>();

        private List<string> temp_edited_Interactions = new List<string>();

        private List<string> temp_selected_Interactions = new List<string>();

        private InteractionFilter temp_interaction_filter = new InteractionFilter();
        private List<InteractionFilter> temp_confirm_cache = new List<InteractionFilter>();


        public List<string> result_interaction_filter = new List<string>();
        // インタラクションのキーになる箇所
        public List<string> result_selected_interactions = new List<string>();
        // インタラクションの値になる箇所
        public InteractionFilter result_interaction_value = new InteractionFilter();

        public void Draw(Rect inRect, List<string> selected_Interactions)
        {
            Listing_Standard listing = Begin(inRect);

            listing.Label(Helper.Label("RCP_IFE_Desc1"));
            listing.Label(Helper.Label("RCP_IFE_Desc2"));
            listing.Label(Helper.Label("RCP_IFE_Desc3"));
            listing.Label(Helper.Label("RCP_IFE_Desc4"));
            listing.Label(Helper.Label("RCP_IFE_Desc5"));
            listing.GapLine();

            if (selected_Interactions.Count <= 0)
            {
                listing.Label(Helper.Label("RCP_IFE_PleaseSelectOne"));
            }
            else
            {
                switch (stage)
                {
                    case 0:
                        CreateOrEditInteractionFilter(listing);
                        break;
                    case 1:
                        EnterInitiatorAndRecipientName(listing);
                        break;
                    case 2:
                        EditInteractionFilter(listing, selected_Interactions);
                        break;
                    case 3:
                        EndEditing(listing);
                        break;
                    case 99:
                        ConfirmCacheDurationOverride(listing);
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
            edit_target_intf = "";
            edit_target_intf_initiator = "";
            edit_target_intf_recipient = "";
            filter_text = "";
            cache_duration_seconds_input_buffer = "12";
            temp_reverse_interaction_filter.Clear();
            //temp_interaction_filter_row.Clear();
            temp_remove_interaction_filter_rows.Clear();
            temp_selected_Interactions.Clear();
            temp_edited_Interactions.Clear();
            temp_interaction_filter = new InteractionFilter();
            result_interaction_filter.Clear();
            result_selected_interactions.Clear();
            result_interaction_value = new InteractionFilter();
        }

        private void CreateOrEditInteractionFilter(Listing_Standard listing)
        {
            listing.Label(Helper.Label("RCP_IFE_CreateInter"));

            ism = PortraitCacheEx.InteractionSelectionMap;

            if (listing.ButtonText(Helper.Label("RCP_B_Create")))
            {
                call_id = "create";
            }

            temp_reverse_interaction_filter.Clear();
            foreach (var kv in ism.InteractionFilter)
            {
                string xml_initiator = Helper.Label("RCP_IFE_Initiator");
                string xml_recipient = Helper.Label("RCP_IFE_Recipient");

                string matched_key = $"{xml_initiator}{kv.Value.matched_initiator_key},{xml_recipient}{kv.Value.matched_recipient_key}";
                if (!temp_reverse_interaction_filter.ContainsKey(matched_key))
                {
                    temp_reverse_interaction_filter[matched_key] = new List<InteractionFilter>();
                }
                // 構造体にすればよかった。影響調査が面倒なのでこのまま
                temp_reverse_interaction_filter[matched_key].Add(new InteractionFilter(kv.Value));
            }

            bool is_cddifferent = false;
            listing.Label(Helper.Label("RCP_IFE_EditBaseInter"));
            foreach (var gf in temp_reverse_interaction_filter)
            {
                if (listing.ButtonText(gf.Key))
                {
                    call_id = "edit";
                    edit_target_intf = gf.Key;
                    temp_edited_Interactions.Clear();
                    temp_confirm_cache.Clear();
                    foreach (var intf in gf.Value)
                    {
                        if (temp_interaction_filter.IsCacheDurationDifferent(intf))
                        {
                            temp_confirm_cache.Add(new InteractionFilter(intf));
                            is_cddifferent = true;
                        }
                        temp_interaction_filter = new InteractionFilter(intf);
                        if (!temp_edited_Interactions.Contains(intf.interaction_name))
                        {
                            temp_edited_Interactions.Add(intf.interaction_name);

                        }

                        if (!temp_selected_Interactions.Contains(intf.interaction_name))
                        {
                            temp_selected_Interactions.Add(intf.interaction_name);
                        }
                    }

                    temp_edited_Interactions.Sort();
                }
            }

            if (call_id == "edit" && is_cddifferent)
            {
                call_id = "edit->confirm cache duration override";
            }
        }

        private void EnterInitiatorAndRecipientName(Listing_Standard listing)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "create->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_IFE_InitiatorInter"));
            Rect input_rect = listing.GetRect(30f);
            edit_target_intf_initiator = Widgets.TextField(input_rect, edit_target_intf_initiator);

            listing.GapLine();

            listing.Label(Helper.Label("RCP_IFE_RecipientInter"));
            Rect input_rect2 = listing.GetRect(30f);
            edit_target_intf_recipient = Widgets.TextField(input_rect2, edit_target_intf_recipient);

            listing.GapLine();

            //listing.Label("この受け手と送り手(どちらかのみでも可能)をまとめる要約名を入力してください:");
            //Rect input_rect3 = listing.GetRect(30f);
            //edit_target_intf_short_label = Widgets.TextField(input_rect3, edit_target_intf_short_label);

            //listing.GapLine();

            listing.Label(Helper.Label("RCP_IFE_EnterInitiatorAndRecipient_Desc1"));
            listing.Label(Helper.Label("RCP_IFE_EnterInitiatorAndRecipient_Desc2"));
            listing.Label(Helper.Label("RCP_IFE_EnterInitiatorAndRecipient_Desc3"));
            Rect input_rect4 = listing.GetRect(30f);
            int rv = edit_target_cache_duration_seconds;
            Widgets.TextFieldNumeric<int>(input_rect4, ref rv, ref cache_duration_seconds_input_buffer);
            edit_target_cache_duration_seconds = rv;
            listing.GapLine();

            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                if (edit_target_intf_initiator != "" || edit_target_intf_recipient != "")
                {
                    call_id = "create->enter name";
                    temp_interaction_filter = new InteractionFilter();
                    if (edit_target_intf_initiator != "")
                    {
                        temp_interaction_filter.is_initiator = true;
                        temp_interaction_filter.matched_initiator_key = edit_target_intf_initiator;
                    }

                    if (edit_target_intf_recipient != "")
                    {
                        temp_interaction_filter.is_recipient = true;
                        temp_interaction_filter.matched_recipient_key = edit_target_intf_recipient;
                    }

                    if (edit_target_cache_duration_seconds < 0)
                    {
                        edit_target_cache_duration_seconds = 12;
                    }

                    temp_interaction_filter.cache_duration_seconds = edit_target_cache_duration_seconds;

                    string xml_initiator = Helper.Label("RCP_IFE_Initiator");
                    string xml_recipient = Helper.Label("RCP_IFE_Recipient");
                    edit_target_intf = $"{xml_initiator}{edit_target_intf_initiator},{xml_recipient}{edit_target_intf_recipient}";

                    //temp_interaction_filter_row.Clear();
                    //temp_interaction_filter_row.Add(interactionFilter);

                    if (temp_reverse_interaction_filter.ContainsKey(edit_target_intf))
                    {
                        temp_edited_Interactions.Clear();

                        foreach (var intf in temp_reverse_interaction_filter[edit_target_intf])
                        {
                            if (!temp_edited_Interactions.Contains(intf.interaction_name))
                            {
                                temp_edited_Interactions.Add(intf.interaction_name);

                            }
                        }
                    }
                }

            }
        }

        private void EditInteractionFilter(Listing_Standard listing, List<string> selected_Interactions)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_IFE_EditInteractionFilter_Desc1"));
            listing.Label(Helper.Label("RCP_IFE_EditInteractionFilter_Desc2"));
            listing.Label(Helper.Label("RCP_IFE_EditInteractionFilter_Desc3"));

            listing.GapLine();

            string xml_ir_name = Helper.Label("RCP_IFE_IRName");
            listing.Label($"{xml_ir_name}{edit_target_intf}");


            listing.GapLine();

            Rect reset_rect = listing.GetRect(30f);
            Widgets.Label(reset_rect.LeftPart(0.6f), Helper.Label("RCP_IFE_ClearSelected"));
            if (Widgets.ButtonText(reset_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Clear")))
            {
                temp_selected_Interactions.Clear();
            }


            listing.Label(Helper.Label("RCP_IFE_InterFilt"));
            Rect filter_rect = listing.GetRect(30f);
            filter_text = Widgets.TextField(filter_rect, filter_text);

            listing.GapLine();

            List<string> merged = selected_Interactions
                .Concat(temp_edited_Interactions)
                .Distinct()
                .ToList();
            merged.Sort();

            List<string> filtered;
            if (string.IsNullOrEmpty(filter_text))
            {
                filtered = merged;
            }
            else
            {
                try
                {
                    Regex regex = new Regex(filter_text, RegexOptions.IgnoreCase);
                    filtered = merged.Where(t => regex.IsMatch(t)).ToList();
                }
                catch (ArgumentException)
                {
                    // 正規表現エラー
                    string xml_regexe = Helper.Label("RCP_IFE_RegexFilter");
                    listing.Label($"{xml_regexe} {filter_text}");
                    filtered = new List<string>();
                }
            }

            foreach (var intr in filtered)
            {
                Rect row_rect = listing.GetRect(30f);

                // ラベル
                Widgets.Label(row_rect.LeftPart(0.6f), intr);

                // SELECT ボタン
                if (Widgets.ButtonText(row_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Select")))
                {
                    if (!temp_selected_Interactions.Contains(intr))
                    {
                        temp_selected_Interactions.Add(intr);
                    }
                    else
                    {
                        temp_selected_Interactions.Remove(intr);
                    }

                }

                // チェックボックス
                bool check_on = temp_selected_Interactions.Contains(intr);
                Rect checkbox_rect = row_rect.RightPart(0.15f);
                Widgets.Checkbox(checkbox_rect.x, checkbox_rect.y, ref check_on);

            }

            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                if (temp_selected_Interactions.Count > 0)
                {
                    result_interaction_filter.Clear();
                    result_selected_interactions.Clear();
                    if (temp_interaction_filter.matched_initiator_key != temp_interaction_filter.matched_recipient_key)
                    {
                        result_interaction_filter.Add(temp_interaction_filter.matched_initiator_key);
                        result_interaction_filter.Add(temp_interaction_filter.matched_recipient_key);
                    }
                    else
                    {
                        result_interaction_filter.Add(temp_interaction_filter.matched_initiator_key);
                    }

                    // インタラクションのキーになる箇所
                    result_selected_interactions = new List<string>(temp_selected_Interactions);
                    // インタラクションの値になる箇所
                    result_interaction_value = temp_interaction_filter;
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

            listing.Label(Helper.Label("RCP_IFE_EndEditting_Desc1"));
            listing.Label(Helper.Label("RCP_IFE_EndEditting_Desc2"));

            listing.GapLine();
            string xml_desc3 = Helper.Label("RCP_IFE_EndEditting_Desc3");
            listing.Label($"{xml_desc3}{edit_target_intf}");
            listing.Label(Helper.Label("RCP_IFE_EndEditting_Desc4"));
            foreach (var kv in temp_selected_Interactions)
            {
                listing.Label($"    ==>{kv}");
            }

            listing.GapLine();
            string xml_desc5 = Helper.Label("RCP_IFE_EndEditting_Desc5");
            listing.Label($"{xml_desc5}{temp_interaction_filter.cache_duration_seconds}");

        }

        private void ConfirmCacheDurationOverride(Listing_Standard listing)
        {
            Rect back_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(back_rect.RightPart(0.55f), Helper.Label("RCP_B_Back")))
            {
                call_id = "edit->confirm cache duration override->back";
            }

            listing.GapLine();

            listing.Label(Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc1"));
            listing.Label(Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc2"));
            listing.Label(Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc3"));
            listing.Label(Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc4"));

            listing.GapLine();
            listing.Label($"{Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc5")} {temp_interaction_filter.cache_duration_seconds}");
            foreach(var tg in temp_confirm_cache)
            {
                listing.Label($"    ==>{Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc6")} {tg.interaction_name} {Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc7")} {tg.matched_recipient_key} {Helper.Label("RCP_IFE_ConfirmCacheDurationOverride_Desc8")} {tg.matched_initiator_key}");
            }
            listing.GapLine();
            Rect enter_rect = listing.GetRect(30f);
            if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
            {
                call_id = "edit";
            }

        }

        private void SetStage()
        {
            if (stage == 0)
            {
                if (call_id == "create")
                {
                    stage = 1;
                }
                else if (edit_target_intf != "" && call_id == "edit")
                {
                    //temp_interaction_filter_row = temp_interaction_filter[edit_target_intf];
                    stage = 2;
                }
                else if (call_id == "edit->confirm cache duration override")
                {
                    stage = 99;
                }
            }
            else if (stage == 1)
            {
                if (call_id == "create->enter name")
                {
                    stage = 2;
                    call_id = "edit";
                }
                else if (call_id == "create->back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 2)
            {
                if (call_id == "edit->back")
                {
                    Reset();
                    stage = 0;
                }
                else if (call_id == "edit->end editing")
                {
                    stage = 3;
                }
            }
            else if (stage == 3)
            {
                if (call_id == "edit->end editing->back")
                {
                    Reset();
                    stage = 0;
                }
            }
            else if (stage == 99)
            {
                if (call_id == "edit->confirm cache duration override->back")
                {
                    Reset();
                    stage = 0;
                }
                else if (edit_target_intf != "" && call_id == "edit")
                {
                    //temp_interaction_filter_row = temp_interaction_filter[edit_target_intf];
                    stage = 2;
                }
            }
        }
    }
}
