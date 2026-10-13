using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static System.Net.Mime.MediaTypeNames;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class InteractionBrowser : TabBase
    {
        private static List<string> all_Interaction = new List<string>();
        private string filter_text = "";
        public List<string> selected_Interactions = new List<string>();

        public void Draw(Rect inRect)
        {
            Listing_Standard listing = Begin(inRect);

            if (all_Interaction.Count == 0) InitializeInteractions();

            Rect reset_rect = listing.GetRect(30f);
            Widgets.Label(reset_rect.LeftPart(0.6f), Helper.Label("RCP_IB_ClearSelected"));
            if (Widgets.ButtonText(reset_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Clear")))
            {
                selected_Interactions.Clear();
            }


            listing.Label(Helper.Label("RCP_IB_InteractionFilter"));
            Rect filter_rect = listing.GetRect(30f);
            filter_text = Widgets.TextField(filter_rect, filter_text);

            listing.GapLine();

            List<string> filtered;
            if (string.IsNullOrEmpty(filter_text))
            {
                filtered = all_Interaction;
            }
            else
            {
                try
                {
                    Regex regex = new Regex(filter_text, RegexOptions.IgnoreCase);
                    filtered = all_Interaction.Where(t => regex.IsMatch(t)).ToList();
                }
                catch (ArgumentException)
                {
                    string re = Helper.Label("RCP_IBE_RegexFilter");
                    // 正規表現エラー
                    listing.Label($"{re} {filter_text}");
                    filtered = new List<string>();
                }
            }

            foreach (var thought in filtered)
            {
                Rect row_rect = listing.GetRect(30f);

                // ラベル
                Widgets.Label(row_rect.LeftPart(0.6f), thought);

                // SELECT ボタン
                if (Widgets.ButtonText(row_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Select")))
                {
                    if (!selected_Interactions.Contains(thought))
                    {
                        selected_Interactions.Add(thought);
                    }
                    else
                    {
                        selected_Interactions.Remove(thought);
                    }

                }

                // チェックボックス
                bool check_on = selected_Interactions.Contains(thought);
                Rect checkbox_rect = row_rect.RightPart(0.15f);
                Widgets.Checkbox(checkbox_rect.x, checkbox_rect.y, ref check_on);

            }

            End(listing);
        }

        public void Reset()
        {
            selected_Interactions.Clear();
        }

        private void InitializeInteractions()
        {
            try
            {
                var allDefs = DefDatabase<InteractionDef>.AllDefs;
                foreach (var def in allDefs)
                {
                    if(def!= null && def.LabelCap != null && def.LabelCap != "")
                    {
                        string lable_cap = def.LabelCap;
                        if (!all_Interaction.Contains(lable_cap))
                        {
                            all_Interaction.Add(lable_cap);
                        }
                    }

                }
                all_Interaction = all_Interaction.Distinct().ToList();
                all_Interaction.Sort();
            }
            catch (Exception)
            {

            }

        }

    }
}
