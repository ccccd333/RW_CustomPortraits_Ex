using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	/*
	[HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard))]
	public static class Patch_DrawCharacterCard {
		public static void Postfix(Rect rect, Pawn pawn, Action randomizeCallback, Rect creationRect, bool showName) {
			if (!pawn.IsColonist || pawn.health.Dead) return;

			float num = CharacterCardUtility.PawnCardSize(pawn).x - 85f;
			num += 40f;
			Rect rect2 = new Rect(num + rect.x, rect.y, 30f, 30f);

			if (Mouse.IsOver(rect2) || DebugViewSettings.drawTooltipEdges) {
				TooltipHandler.TipRegion(rect2, Helper.Label("SelectPortrait"));
			}
			if (Widgets.ButtonImage(rect2, TexButton.Search)) {
				Dialog_PortraitPawn.Open(pawn);
			}
		}
	}*/
}
