using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public static class PortraitDrawer {
		private static readonly MethodInfo getterTabRect = AccessTools.DeclaredPropertyGetter(typeof(InspectTabBase), "TabRect");
		private static Rect GetTabRect(this InspectTabBase tab) {
			return (Rect)getterTabRect.Invoke(tab, new object[] { });
		}

		public static void DrawNextToInspector(Texture2D tex, float paneTopY, InspectTabBase tab) {
			Rect rect;
			if (tab == null) {
				rect = GetPortraitRect(TextAnchor.LowerLeft, 0, paneTopY - 30f);
			} else {
				Rect tabRect = tab.GetTabRect();
				switch (StaticSettings.TabPosition) {
					case PortraitTabPosition.Left:
						rect = GetPortraitRect(TextAnchor.LowerLeft, tabRect.x, tabRect.y);
						break;
					case PortraitTabPosition.Top:
						rect = GetPortraitRect(TextAnchor.LowerRight, tabRect.xMax, tabRect.y);
						break;
					case PortraitTabPosition.Right:
						rect = GetPortraitRect(TextAnchor.UpperLeft, tabRect.xMax, tabRect.y);
						break;
					default:
					case PortraitTabPosition.Bottom:
						rect = GetPortraitRect(TextAnchor.LowerLeft, tabRect.xMax, tabRect.yMax);
						break;
				}
			}
			DrawTexture(rect, tex);
		}

		public static void DrawTopRight(Texture2D tex) {
			Rect rect = GetPortraitRect(TextAnchor.UpperRight, UI.screenWidth, 0);
			DrawTexture(rect, tex);
		}

		public static void DrawActions(Texture2D tex) {
			float paneWidth = InspectPaneUtility.PaneWidthFor(Find.WindowStack.WindowOfType<IInspectPane>());
			float y = UI.screenHeight - 35f - StaticSettings.OffsetY;
			Rect rect = GetPortraitRect(TextAnchor.LowerLeft, paneWidth + StaticSettings.OffsetX, y, StaticSettings.ActionsBig);
			DrawTexture(rect, tex);
		}

		public static void DrawCustom(Texture2D tex) {
			Rect rect = GetPortraitRect(StaticSettings.CustomAnchor, StaticSettings.CustomX, StaticSettings.CustomY);
			DrawTexture(rect, tex);
		}

		public static void DrawColonistBar(Rect rect, Texture2D tex) {
			GUI.DrawTexture(rect.ContractedBy(1f), tex);
		}

		private static void DrawTexture(Rect rect, Texture2D tex) {
			Widgets.ButtonImage(rect, tex, Color.white, Color.white, false);
		}

		private static Rect GetPortraitRect(TextAnchor anchor, float x, float y, bool handleHover = true) {
			Rect rect = AlignRect(anchor, x, y, StaticSettings.SmallWidth, StaticSettings.SmallHeight);
			if (handleHover && Mouse.IsOver(rect))
				rect = AlignRect(anchor, x, y, StaticSettings.BigWidth, StaticSettings.BigHeight);
			return rect;
		}

		private static Rect AlignRect(TextAnchor anchor, float x, float y, float width, float height) {
			Rect rect = new Rect(0, 0, width, height);
			switch (anchor) {
				default:
				case TextAnchor.UpperLeft:
					rect.x = x;
					rect.y = y;
					break;
				case TextAnchor.UpperCenter:
					rect.x = x - width / 2;
					rect.y = y;
					break;
				case TextAnchor.UpperRight:
					rect.x = x - width;
					rect.y = y;
					break;
				case TextAnchor.MiddleLeft:
					rect.x = x;
					rect.y = y - height / 2;
					break;
				case TextAnchor.MiddleCenter:
					rect.x = x - width / 2;
					rect.y = y - height / 2;
					break;
				case TextAnchor.MiddleRight:
					rect.x = x - width;
					rect.y = y - height / 2;
					break;
				case TextAnchor.LowerLeft:
					rect.x = x;
					rect.y = y - height;
					break;
				case TextAnchor.LowerCenter:
					rect.x = x - width / 2;
					rect.y = y - height;
					break;
				case TextAnchor.LowerRight:
					rect.x = x - width;
					rect.y = y - height;
					break;
			}
			return rect;
		}
	}
}
