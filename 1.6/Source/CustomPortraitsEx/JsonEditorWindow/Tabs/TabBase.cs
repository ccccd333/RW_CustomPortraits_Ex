using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.JsonEditorWindow.Tabs
{
    public class TabBase
    {
        private float scroll_height = 0;
        private Vector2 scroll;
        public string call_id = "";
        public Listing_Standard Begin(Rect inRect)
        {
            Rect base_rect = inRect.BottomPart(0.95f);
            Rect view_rect = new Rect(0, 0, inRect.width - 17f, scroll_height);
            Widgets.BeginScrollView(base_rect, ref scroll, view_rect);
            Listing_Standard listing = new Listing_Standard(inRect.AtZero(), () => scroll)
            {
                maxOneColumn = true,
                ColumnWidth = view_rect.width
            };
            listing.Begin(view_rect);

            return listing;
        }

        public void End(Listing_Standard listing)
        {
            if (Event.current.type == EventType.Layout)
            {
                scroll_height = listing.CurHeight;
            }

            listing.End();
            Widgets.EndScrollView();
        }
    }
}
