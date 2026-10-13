using Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow
{
    [StaticConstructorOnStartup]
    public class ReloadJsonWithErrorsWindow : Mod
    {
        private static int tab_int = 0;
        private List<TabRecord> Tabs = new List<TabRecord>();
        private PresetStatusBrowser PresetStatusBrowser = new PresetStatusBrowser();
        private PresetErrorReloader PresetErrorReloader = new PresetErrorReloader();
        private PresetErrorBrowser PresetErrorBrowser = new PresetErrorBrowser();
        public ReloadJsonWithErrorsWindow(ModContentPack content) : base(content) { }

        public override string SettingsCategory()
        {
            return Helper.Label("RCPReloadJSONAndCheckErrors");
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
                Tabs.Add(new TabRecord(Helper.Label("RCPRJACE_EditorTab1"), () =>
                {
                    SetTabInt(0);
                }, tab_int == 0));
                Tabs.Add(new TabRecord(Helper.Label("RCPRJACE_EditorTab2"), () =>
                {
                    SetTabInt(1);
                }, tab_int == 1));
                Tabs.Add(new TabRecord(Helper.Label("RCPRJACE_EditorTab3"), () =>
                {
                    SetTabInt(2);
                }, tab_int == 2));
            }
            TabDrawer.DrawTabs(tabsRect, Tabs);


            switch (tab_int)
            {
                case 0:
                    Tabs[0].selected = true;
                    PresetStatusBrowser.Draw(inRect);
                    break;
                case 1:
                    Tabs[1].selected = true;
                    PresetErrorReloader.Draw(inRect);
                    break;
                case 2:
                    Tabs[2].selected = true;
                    PresetErrorBrowser.Draw(inRect);
                    break;

            }
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
    }
}
