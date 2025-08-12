using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawColonist))]
	public static class Patch_DrawColonist {
		private static readonly MethodInfo methodStart = AccessTools.Method(typeof(ColonistBarColonistDrawer), "DrawCaravanSelectionOverlayOnGUI");
		private static readonly MethodInfo methodEnd = AccessTools.Method(typeof(PortraitsCache), "Get");

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			List<CodeInstruction> list = instructions.ToList();

			if(ModCompatibility.OwlsColonistBar) {
				Log.Message("[Portraits] Owl's Colonist Bar detected. Not patching Vanilla DrawColonist.");
				return list;
			}
			if (ModCompatibility.NalsCustomPortraits) {
				Log.Message("[Portraits] Nals' Custom Portraits detected. Not patching Vanilla DrawColonist.");
				return list;
			}

			int index = list.FindIndex(x => x.Calls(methodStart)) + 1;
			int end = list.FindIndex(index, x => x.Calls(methodEnd)) + 1;

			if (index <= 0) {
				Log.Error("[Portraits] Failed to transpile RimWorld.ColonistBarColonistDrawer.DrawColonist: injection start index not found");
				return list;
			}
			if (end <= 0) {
				Log.Error("[Portraits] Failed to transpile RimWorld.ColonistBarColonistDrawer.DrawColonist: injection end index not found");
				return list;
			}

			Label labelSkip = il.DefineLabel();
			Label labelEnd = il.DefineLabel();
			CodeInstruction start = new CodeInstruction(OpCodes.Ldarg_2).MoveLabelsFrom(list[index]);
			list[end + 1].labels.Add(labelEnd);
			list[index].labels.Add(labelSkip);
			list.Insert(index++, start);
			list.Insert(index++, new CodeInstruction(OpCodes.Call, Helper.ShouldDrawColonistBarMethod));
			list.Insert(index++, new CodeInstruction(OpCodes.Brfalse_S, labelSkip));
			list.Insert(index++, new CodeInstruction(OpCodes.Ldarg_1));
			list.Insert(index++, new CodeInstruction(OpCodes.Ldarg_2));
			list.Insert(index++, new CodeInstruction(OpCodes.Call, Helper.DrawColonistBarMethod));
			list.Insert(index++, new CodeInstruction(OpCodes.Br_S, labelEnd));
			return list;
		}
	}
}
