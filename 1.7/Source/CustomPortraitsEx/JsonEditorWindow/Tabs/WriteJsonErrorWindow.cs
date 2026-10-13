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
    public class WriteJsonErrorWindow : TabBase
    {
        int stage = 0;
        public void Draw(Rect inRect, string preset_name, List<string> error_message1, List<string> error_message2)
        {
            Listing_Standard listing = Begin(inRect);



            //if (error_message1.Count == 0 && error_message2.Count == 0)
            //{
            //    call_id = "end";
            //}
            //else
            {
                Rect enter_rect = listing.GetRect(30f);
                if (Widgets.ButtonText(enter_rect.RightPart(0.55f).LeftPart(0.7f), Helper.Label("RCP_B_Enter")))
                {
                    call_id = "end";
                }
                else
                {
                    if (error_message1.Count == 0 && error_message2.Count == 0)
                    {
                        listing.Label(Helper.Label("RCP_WJEW_Desc1"));
                        call_id = "no error";
                    }
                    else
                    {
                        listing.Label(Helper.Label("RCP_WJEW_Desc2"));
                        listing.Label(Helper.Label("RCP_WJEW_Desc3"));
                        listing.Label(Helper.Label("RCP_WJEW_Desc4"));
                        listing.Label(Helper.Label("RCP_WJEW_Desc5"));
                        listing.GapLine();

                        listing.Label($"{Helper.Label("RCP_WJEW_Desc6")} ==> [{preset_name}]");
                        foreach (string error in error_message1)
                        {
                            listing.Label($"{Helper.Label("RCP_WJEW_Desc7")} ==> [{error}]");
                        }

                        listing.GapLine();

                        listing.Label($"{Helper.Label("RCP_WJEW_Desc6")} ==> [InteractionFilter.json]");
                        foreach (string error in error_message2)
                        {
                            listing.Label($"{Helper.Label("RCP_WJEW_Desc7")} ==> [{error}]");

                        }

                        call_id = "error";
                    }

                }
            }

            End(listing);
        }

        public void Reset()
        {
            call_id = "";
        }
    }
}
