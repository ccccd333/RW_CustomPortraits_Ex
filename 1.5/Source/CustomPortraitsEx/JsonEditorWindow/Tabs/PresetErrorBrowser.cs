using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class PresetErrorBrowser :TabBase
    {
        int stage = 0;

        public void Draw(Rect inRect)
        {
            Listing_Standard listing = Begin(inRect);

            switch (stage)
            {
                case 0:
                    DrawErrorLogs(listing);
                    break;
            }

            SetStage();

            End(listing);
        }

        public void DrawErrorLogs(Listing_Standard listing)
        {
            if (PortraitCacheEx.Refs.Count == 0 && PortraitCacheEx.PresetErrorMap.Count == 0)
            {
                listing.Label(Helper.Label("RCPRJACE_PEB_Desc1"));
            }
            else
            {
                foreach (var item in PortraitCacheEx.PresetErrorMap)
                {
                    listing.Label($"{Helper.Label("RCPRJACE_PEB_Desc2")}{item.Key}");
                    listing.GapLine();

                    for(int i = 0; i < item.Value.Count; i++)
                    {
                        listing.Label($"[{i}]==>{item.Value[i]}");
                    }
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
