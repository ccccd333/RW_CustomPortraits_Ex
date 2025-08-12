using Foxy.CustomPortraits.CustomPortraitsEx;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(InspectPaneUtility), nameof(InspectPaneUtility.ExtraOnGUI))]
	public static class Patch_ExtraOnGUI {
		public static void Prefix(IInspectPane pane) {
			if (!pane.AnythingSelected) return;
			Pawn pawn = Helper.GetSelectedPawn();
			if (pawn is null) return;

			if (StaticSettings.IsInspector) {
				InspectTabBase tab = StaticSettings.ShowWhenInspector ? pane.GetOpenTab() : null;
				Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.Inspector);
                string filename = pawn.GetPortraitName(PortraitPosition.Inspector);

                tex = ConditionDrivenPortrait.GetPortraitTexture(pawn, filename, tex);
                if (tex != null) PortraitDrawer.DrawNextToInspector(tex, pane.PaneTopY, tab);
			}
			if (StaticSettings.IsTopRight) {
				Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.TopRight);
				if (tex != null) PortraitDrawer.DrawTopRight(tex);
			}
			if (StaticSettings.IsCustom) {
				Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.Custom);
				if (tex != null) PortraitDrawer.DrawCustom(tex);
			}
		}
	}
}
