using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class PresetStatusBrowser : TabBase
    {
        int stage = 0;

        public void Draw(Rect inRect)
        {
            Listing_Standard listing = Begin(inRect);

            switch (stage)
            {
                case 0:
                    DrawPresetStatus(listing);
                    break;
            }

            SetStage();

            End(listing);
        }

        public void DrawPresetStatus(Listing_Standard listing)
        {
            if(PortraitCacheEx.Refs.Count == 0)
            {
                listing.Label(Helper.Label("RCPRJACE_PSB_Desc1"));
            }
            else
            {
                listing.Label(Helper.Label("RCPRJACE_PSB_Desc2"));
                listing.GapLine();
                foreach (var item in PortraitCacheEx.Refs)
                {
                    Rect is_preset_rect = listing.GetRect(30f);
                    Widgets.Label(is_preset_rect.LeftPart(0.6f), $"{Helper.Label("RCPRJACE_PSB_Desc3")}{item.Key}");

                    bool is_error = !PortraitCacheEx.PresetErrorMap.ContainsKey(item.Key);
                    Rect checkbox_rect = is_preset_rect.RightPart(0.15f);
                    Widgets.Checkbox(checkbox_rect.x, checkbox_rect.y, ref is_error);
                }
            }
        }

        public void Reset()
        {
            stage = 0;
        }

        private void SetStage()
        {

        }
    }
}
