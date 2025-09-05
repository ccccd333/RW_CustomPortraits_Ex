using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public static class Helper {
		private static readonly MethodInfo helperDraw = AccessTools.Method(typeof(Helper), nameof(DrawPortrait));
		private static readonly MethodInfo helperCond = AccessTools.Method(typeof(Helper), nameof(ShouldInterceptDraw));

		public static MethodInfo DrawColonistBarMethod => helperDraw;
		public static MethodInfo ShouldDrawColonistBarMethod => helperCond;

		public static Pawn GetSelectedPawn() {
			UIRoot_Play uiRoot = Find.UIRoot as UIRoot_Play;
			if (uiRoot == null) return null;
			Thing thing = uiRoot.mapUI.selector.SingleSelectedThing;
			if (thing == null) return null;
			if (thing is Corpse corpse) return corpse.InnerPawn;
			return thing as Pawn;
		}

		public static string Label(string key) {
			return $"Foxy.CustomPortraits.{key}".Translate().Trim();
		}
		public static string Label(string key, NamedArgument arg) {
			return $"Foxy.CustomPortraits.{key}".Translate(arg).Trim();
		}
		public static void OpenDialog(Pawn pawn) {
			if (StaticSettings.Advanced) Dialog_AdvancedPortraitPawn.Open(pawn);
			else Dialog_SimplePortraitPawn.Open(pawn);
		}

		public static Action<Rect> GetTooltipDelegate(string text) {
			return rect => TooltipHandler.TipRegion(rect, text);
		}

		private static void DrawPortrait(Rect rect, Pawn pawn) {
			if (pawn is null) return;
			Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.ColonistBar);
			if (tex == null) return;
			PortraitDrawer.DrawColonistBar(rect, tex);
		}
		private static bool ShouldInterceptDraw(Pawn pawn) {
			if (!StaticSettings.IsColonistBar) return false;
			return pawn != null && pawn.GetPortraitTexture(PortraitPosition.ColonistBar) != null;
		}
	}
}
