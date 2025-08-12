using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public static class Patch_Mod_OwlsColonistBar {
		private static FieldInfo fieldFrames = null;
		private static FieldInfo fieldFrameLoops = null;
		private static readonly MethodInfo helperGet = AccessTools.Method(typeof(Patch_Mod_OwlsColonistBar), nameof(Patch_Mod_OwlsColonistBar.GetPortraitTexture));
		private static readonly Dictionary<Pawn, RenderTexture> cache = new Dictionary<Pawn, RenderTexture>();

		public static void PatchAll(Harmony h) {
			Log.Message("[Portraits] OwlsColonistBar mod detected.");
			Log.Warning("[Portraits] Not patching for mod compat - not supported on 1.6 yet.");
			// Patch_PawnCache(h);
		}

		private static void Patch_PawnCache(Harmony h) {
			ConstructorInfo method = AccessTools.TypeByName("OwlBar.PawnCache").GetConstructors(AccessTools.all).First();
			HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(Patch_Mod_OwlsColonistBar), nameof(Patch_Mod_OwlsColonistBar.Transpiler_PawnCache)));
			h.Patch(method, transpiler: transpiler);
			Log.Message("[Portraits] Patched OwlBar.PawnCache..ctor");
		}

		private static IEnumerable<CodeInstruction> Transpiler_PawnCache(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo method = AccessTools.Method(typeof(PortraitsCache), "Get");

			int index = list.FindIndex(x => x.Calls(method));
			if (index < 0) {
				Log.Error("[Portraits] Failed to transpile OwlBar.PawnCache..ctor: injection start index not found");
				return list;
			}

			list[index] = new CodeInstruction(OpCodes.Call, helperGet);

			return list;
		}

		public static void ResetCache() {
			if (fieldFrames == null) {
				fieldFrames = AccessTools.Field("OwlBar.OwlColonistBar:frames");
				fieldFrameLoops = AccessTools.Field("OwlBar.OwlColonistBar:frameLoops");
			}
			fieldFrames.SetValue(null, 120);
			fieldFrameLoops.SetValue(null, 19);
			foreach (RenderTexture tex in cache.Values) Object.Destroy(tex);
			cache.Clear();
		}

		private static RenderTexture GetPortraitTexture(Pawn pawn, Vector2 size, Rot4 rotation, Vector3 cameraOffset, float cameraZoom, bool supersample, bool compensateForUIScale, bool renderHeadgear, bool renderClothes, IReadOnlyDictionary<Apparel, Color> overrideApparelColors, Color? overrideHairColor, bool stylingStation, PawnHealthState? healthStateOverride) {
			if (pawn != null && StaticSettings.IsColonistBar) {
				if (cache.TryGetValue(pawn, out RenderTexture cached)) return cached;
				Texture2D portrait = pawn.GetPortraitTexture(PortraitPosition.ColonistBar);
				if (portrait != null) {
					RenderTexture tex = new RenderTexture(Mathf.FloorToInt(size.x), Mathf.FloorToInt(size.y), 32);
					RenderTexture.active = tex;
					GL.PushMatrix();
					GL.LoadPixelMatrix(0, size.x, size.y, 0);
					Graphics.DrawTexture(new Rect(0, size.y - size.x, size.x, size.x), portrait);
					GL.PopMatrix();
					RenderTexture.active = null;
					cache[pawn] = tex;
					return tex;
				}
			}
			return PortraitsCache.Get(pawn, size, rotation, cameraOffset, cameraZoom, supersample, compensateForUIScale, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation, healthStateOverride);
		}
	}
}
