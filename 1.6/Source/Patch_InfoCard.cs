using HarmonyLib;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DoWindowContents))]
	public static class Patch_InfoCard {
		public static void Prefix(Rect inRect, Thing ___thing) {
			if (___thing == null) return;

			Pawn pawn = ___thing as Pawn;
			if (pawn == null || !IsSuitableForPortrait(pawn)) return;

			Rect rect = new Rect(inRect.width - 44f, inRect.y + 4f, 18f, 18f);

			if (Mouse.IsOver(rect) || DebugViewSettings.drawTooltipEdges) {
				TooltipHandler.TipRegion(rect, Helper.Label("SelectPortrait"));
			}
			if (Widgets.ButtonImage(rect, TexButton.Search)) {
				Dialog_PortraitPawn.Open(pawn);
			}
		}

		private static bool IsSuitableForPortrait(Pawn pawn) {
			if (pawn.IsColonist && !pawn.health.Dead) return true;
			if (pawn.RaceProps.Animal) return true;
			return false;
		}
	}
}
