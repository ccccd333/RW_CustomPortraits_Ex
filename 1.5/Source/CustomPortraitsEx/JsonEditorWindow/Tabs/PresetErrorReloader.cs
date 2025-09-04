using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class PresetErrorReloader : TabBase
    {
        int stage = 0;
        string reload_target_preset_name = "";

        public void Draw(Rect inRect)
        {
            Listing_Standard listing = Begin(inRect);

            switch (stage)
            {
                case 0:
                    ReloadErroredPresets(listing);
                    break;
                case 1:
                    ReloadJson(listing);
                    break;
            }

            SetStage();

            End(listing);
        }

        public void Reset()
        {
            stage = 0;
            reload_target_preset_name = "";
        }

        private void ReloadErroredPresets(Listing_Standard listing)
        {
            if (PortraitCacheEx.Refs.Count == 0 && PortraitCacheEx.PresetErrorMap.Count == 0)
            {
                listing.Label(Helper.Label("RCPRJACE_PER_Desc1"));
            }
            else
            {
                listing.Label(Helper.Label("RCPRJACE_PER_Desc2"));
                listing.GapLine();
                foreach (var item in PortraitCacheEx.Refs)
                {
                    if (PortraitCacheEx.PresetErrorMap.ContainsKey(item.Key))
                    {
                        if (listing.ButtonText(item.Key))
                        {
                            call_id = "reload";
                            reload_target_preset_name = item.Key;
                        }
                    }
                }
            }
        }

        private void ReloadJson(Listing_Standard listing)
        {
            listing.Label(Helper.Label("RCPRJACE_PER_Desc3"));
            PortraitCacheEx.ReadPresetJson(reload_target_preset_name);

            call_id = "end";
        }



        private void SetStage()
        {
            if (stage == 0)
            {
                if (call_id == "reload")
                {
                    stage = 1;
                }
            }
            else if (stage == 1)
            {
                if (call_id == "end")
                {
                    stage = 0;
                    Reset();
                }
            }
        }
    }
}
