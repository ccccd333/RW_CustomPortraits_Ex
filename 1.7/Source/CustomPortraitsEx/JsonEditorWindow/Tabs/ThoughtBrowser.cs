using Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow
{
    public class ThoughtBrowser : TabBase
    {
        private static List<string> all_thoughts = new List<string>();
        private string filter_text = "";
        public List<string> selected_thoughts = new List<string>();

        public void Draw(Rect inRect)
        {
            Listing_Standard listing = Begin(inRect);

            if (all_thoughts.Count == 0) InitializeThoughts();

            Rect reset_rect = listing.GetRect(30f);
            Widgets.Label(reset_rect.LeftPart(0.6f), Helper.Label("RCP_TB_ClearSelected"));
            if (Widgets.ButtonText(reset_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Clear")))
            {
                selected_thoughts.Clear();
            }

            listing.Label(Helper.Label("RCP_TB_ThoughtFilter"));
            Rect filter_rect = listing.GetRect(30f);
            filter_text = Widgets.TextField(filter_rect, filter_text);

            listing.GapLine();

            List<string> filtered;
            if (string.IsNullOrEmpty(filter_text))
            {
                filtered = all_thoughts;
            }
            else
            {
                try
                {
                    Regex regex = new Regex(filter_text, RegexOptions.IgnoreCase);
                    filtered = all_thoughts.Where(t => regex.IsMatch(t)).ToList();
                }
                catch (ArgumentException)
                {
                    // 正規表現エラー
                    string re = Helper.Label("RCP_TBE_RegexFilter");
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
                    if (!selected_thoughts.Contains(thought))
                    {
                        selected_thoughts.Add(thought);
                    }
                    else
                    {
                        selected_thoughts.Remove(thought);
                    }

                    //selected_thought = thought;
                    //Log.Message("Selected: " + selected_thought);
                }

                // チェックボックス
                bool check_on = selected_thoughts.Contains(thought);
                Rect checkbox_rect = row_rect.RightPart(0.15f);
                Widgets.Checkbox(checkbox_rect.x, checkbox_rect.y, ref check_on);

            }

            End(listing);

        }

        public void Reset()
        {
            selected_thoughts.Clear();
        }


        private void InitializeThoughts()
        {
            // Assembly-Csharp
            // DebugOutputsPawns.Thoughts
            // dnspy
            try
            {
                var allDefs = DefDatabase<ThoughtDef>.AllDefs;
                foreach (var def in allDefs)
                {

                    ThoughtStagesTexts(def, all_thoughts);

                }
                all_thoughts = all_thoughts.Distinct().ToList();
                all_thoughts.Sort();
            }
            catch (Exception)
            {

            }

        }

        private void ThoughtStagesTexts(ThoughtDef t, List<string> all_thoughts)
        {
            // Assembly-Csharp
            // DebugOutputsPawns.ThoughtStagesText()
            // dnspy
            if (t.stages == null)
            {
                return;
            }
            for (int i = 0; i < t.stages.Count; i++)
            {
                string text = "";
                ThoughtStage thoughtStage = t.stages[i];
                //text = string.Concat(new object[] { text, "[", i, "] " });
                if (thoughtStage == null)
                {
                    //text += "null";
                    continue;
                }
                else
                {
                    if (thoughtStage.label != null)
                    {
                        text += thoughtStage.label;
                    }
                    if (thoughtStage.labelSocial != null)
                    {
                        if (thoughtStage.label != null)
                        {
                            text += "/";
                        }
                        text += thoughtStage.labelSocial;
                    }

                    if (!all_thoughts.Contains(text) && text != "")
                    {
                        string regex_pattern = Regex.Replace(text, @"\s*\{\d+\}\s*", ".*");

                        regex_pattern = Regex.Replace(regex_pattern, @"\s*\{[A-Za-z_][A-Za-z0-9_]*\}\s*", ".*");
                        all_thoughts.Add(regex_pattern);
                    }

                    //text += " ";
                    //if (thoughtStage.baseMoodEffect != 0f)
                    //{
                    //    text = text + "[" + thoughtStage.baseMoodEffect.ToStringWithSign("0.##") + " Mo]";
                    //}
                    //if (thoughtStage.baseOpinionOffset != 0f)
                    //{
                    //    text = text + "(" + thoughtStage.baseOpinionOffset.ToStringWithSign("0.##") + " Op)";
                    //}
                }
                //if (i < t.stages.Count - 1)
                //{
                //    text += "\n";
                //}
            }
            return;
        }
    }
}
