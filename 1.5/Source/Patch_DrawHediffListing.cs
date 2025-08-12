using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(HealthCardUtility), nameof(HealthCardUtility.DrawHediffListing))]
	public static class Patch_DrawHediffListing {
		public static void Prefix(Rect rect, Pawn pawn) {
			Rect btn = new Rect(rect.x + rect.width - 38f - 8f, rect.y - 27f + 2f, 20f, 20f);
			if (Mouse.IsOver(btn) || DebugViewSettings.drawTooltipEdges) {
				TooltipHandler.TipRegion(btn, Helper.Label("SelectPortrait"));
			}
			if (Widgets.ButtonImage(btn, TexButton.Search)) {
				Dialog_PortraitPawn.Open(pawn);
			}
		}
	}
}
