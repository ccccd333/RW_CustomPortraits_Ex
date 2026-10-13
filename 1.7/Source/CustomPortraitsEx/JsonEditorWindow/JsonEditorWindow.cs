using CustomPortraits;
using Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs;
using Newtonsoft.Json.Linq;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow
{
    [StaticConstructorOnStartup]
    public class JsonEditorWindow : Mod
    {


        private static int tab_int = 0;
        private ThoughtBrowser ThoughtBrowser = new ThoughtBrowser();
        private DashboardTab DashboardTab = new DashboardTab();
        private InteractionBrowser InteractionBrowser = new InteractionBrowser();
        private InteractionFilterEditor InteractionFilterEditor = new InteractionFilterEditor();
        private GroupEditor GroupEditor = new GroupEditor();
        private PortraitGroupEditor PortraitGroupEditor = new PortraitGroupEditor();
        private PriorityWeightEditor PriorityWeightEditor = new PriorityWeightEditor();
        private List<TabRecord> Tabs = new List<TabRecord>();

        private CustomPortraitJsonWriter CustomPortraitJsonWriter = new CustomPortraitJsonWriter();
        private WriteJsonErrorWindow WriteJsonErrorWindow = new WriteJsonErrorWindow();

        List<string> error_message1 = new List<string>();
        List<string> error_message2 = new List<string>();
        public JsonEditorWindow(ModContentPack content) : base(content) { }

        public override string SettingsCategory()
        {
            
            return Helper.Label("RCPEditor");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float height = inRect.height * 0.04f;
            Rect tabsRect = new Rect(inRect.x + inRect.width * 0.06f, inRect.y + height, inRect.width * 0.94f, height);
            if (Tabs == null)
            {
                Tabs = new List<TabRecord>();
            }
            if (Tabs.Empty())
            {
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab1"), () =>
                {
                    SetTabInt(0);
                }, tab_int == 0));
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab2"), () =>
                {
                    SetTabInt(1);
                }, tab_int == 1));
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab3"), () =>
                {
                    SetTabInt(2);
                }, tab_int == 2));
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab4"), () =>
                {
                    SetTabInt(3);
                }, tab_int == 3));
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab5"), () =>
                {
                    SetTabInt(4);
                }, tab_int == 4));
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab6"), () =>
                {
                    SetTabInt(5);
                }, tab_int == 5));
                Tabs.Add(new TabRecord(Helper.Label("RCPEditorTab7"), () =>
                {
                    SetTabInt(6);
                }, tab_int == 6));
            }
            TabDrawer.DrawTabs(tabsRect, Tabs);


            //Rect viewRect = new Rect(0, 0, inRect.width - 17f, scrollHeight);
            //Widgets.BeginScrollView(inRect, ref scroll, viewRect);
            //Listing_Standard listing = new Listing_Standard(inRect.AtZero(), () => scroll)
            //{
            //    maxOneColumn = true,
            //    ColumnWidth = viewRect.width
            //};
            //listing.Begin(viewRect);

            //----------------------Begin

            //if (!PortraitCacheEx.IsAvailable)
            //{
            //    //listing.Label("RCPError".Translate());
            //}
            //else
            {
                if (DashboardTab.json_write_modes.Contains(DashboardTab.WRITE_JSON_ALL) ||
                    DashboardTab.json_write_modes.Contains(DashboardTab.WRITE_NEW_JSON_ALL) ||
                    DashboardTab.json_write_modes.Contains(DashboardTab.WRITE_JSON_PW) ||
                    DashboardTab.json_write_modes.Contains(DashboardTab.WRITE_JSON_INTERACTIONS))
                {
                    if (WriteJsonErrorWindow.call_id == "")
                    {
                        CustomPortraitJsonWriter.Write(DashboardTab, InteractionFilterEditor, GroupEditor, PortraitGroupEditor, PriorityWeightEditor);

                        error_message1 = PortraitCacheEx.ReadPresetJson(DashboardTab.selected_preset_name);
                        if (DashboardTab.json_write_modes.Contains(DashboardTab.WRITE_JSON_INTERACTIONS))
                        {
                            error_message2 = PortraitCacheEx.ReadPresetJson("InteractionFilter");
                        }

                        WriteJsonErrorWindow.Draw(inRect, DashboardTab.selected_preset_name, error_message1, error_message2);
                    }
                    else if (WriteJsonErrorWindow.call_id == "end")
                    {
                        ResetAll();
                    }
                    else
                    {
                        WriteJsonErrorWindow.Draw(inRect, DashboardTab.selected_preset_name, error_message1, error_message2);
                    }


                }
                else if (DashboardTab.change_preset)
                {
                    ResetExceptDashboardTab();
                }
                else
                {

                    switch (tab_int)
                    {
                        case 0:
                            Tabs[0].selected = true;

                            bool ife_e = InteractionFilterEditor.call_id == "edit->end editing";
                            bool ge_e = GroupEditor.call_id == "edit->end editing";
                            bool pge_e = PortraitGroupEditor.call_id == "edit end";
                            bool pw_e = PriorityWeightEditor.call_id == "order end->weight end";

                            Dictionary<string, bool> end_flags = new Dictionary<string, bool> {
                            { "InteractionFilterEditor", ife_e }, {"GroupEditor",ge_e }, {"PortraitGroupEditor",pge_e }, {"PriorityWeightEditor",pw_e } };
                            DashboardTab.Draw(inRect, end_flags);
                            break;
                        case 1:
                            Tabs[1].selected = true;
                            ThoughtBrowser.Draw(inRect);
                            break;
                        case 2:
                            Tabs[2].selected = true;
                            InteractionBrowser.Draw(inRect);
                            break;
                        case 3:
                            Tabs[3].selected = true;
                            InteractionFilterEditor.Draw(inRect, InteractionBrowser.selected_Interactions);
                            break;
                        case 4:
                            Tabs[4].selected = true;
                            GroupEditor.Draw(inRect, ThoughtBrowser.selected_thoughts, InteractionBrowser.selected_Interactions, InteractionFilterEditor.result_interaction_filter, DashboardTab.selected_preset_name);
                            break;
                        case 5:
                            Tabs[5].selected = true;
                            PortraitGroupEditor.Draw(inRect, GroupEditor.result_edit_target_group_name, DashboardTab.selected_preset_name);
                            break;
                        case 6:
                            Tabs[6].selected = true;
                            PriorityWeightEditor.Draw(inRect, GroupEditor.result_edit_target_group_name, DashboardTab.selected_preset_name);
                            break;
                    }
                }
            }

            //----------------------End
            //if (Event.current.type == EventType.Layout)
            //{
            //    scrollHeight = listing.CurHeight;
            //}

            //listing.End();
            //Widgets.EndScrollView();
        }

        private void SetTabInt(int i)
        {
            tab_int = i;
            if (i > Tabs.Count - 1)
            {
                return;
            }
            for (int a = 0; a < Tabs.Count; a++)
            {
                if (a == i)
                {
                    Tabs[a].selected = true;
                }
                else
                {
                    Tabs[a].selected = false;
                }
            }
        }

        private void ResetAll()
        {
            DashboardTab.Reset();
            InteractionFilterEditor.Reset();
            GroupEditor.Reset();
            PortraitGroupEditor.Reset();
            PriorityWeightEditor.Reset();
            WriteJsonErrorWindow.Reset();
            error_message1.Clear();
            error_message2.Clear();
        }

        private void ResetExceptDashboardTab()
        {
            DashboardTab.change_preset = false;
            InteractionFilterEditor.Reset();
            GroupEditor.Reset();
            PortraitGroupEditor.Reset();
            PriorityWeightEditor.Reset();
            error_message1.Clear();
            error_message2.Clear();
        }
    }
}
