using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(HealthCardUtility), "DoDebugOptions")]
	public static class Patch_DoDebugOptions {
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			int index = list.FindIndex(x => x.LoadsConstant(240f));
			if(index < 0) {
				Log.Error("[Portraits] Failed to transpile HealthCardUtility.DoDebugOptions: injection start index not found");
				return instructions;
			}

			list[index] = new CodeInstruction(OpCodes.Ldc_R4, 200f);

			return list;
		}
	}
}
