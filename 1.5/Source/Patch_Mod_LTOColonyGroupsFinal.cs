using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Foxy.CustomPortraits {
	public static class Patch_Mod_LTOColonyGroupsFinal {
		public static void PatchAll(Harmony h) {
			Log.Message("[Portraits] Colony Groups mod detected.");
			Patch_ColonistGroup(h);
			Patch_ColonistBarColonistDrawer(h);
		}

		private static void Patch_ColonistGroup(Harmony h) {
			MethodInfo method = AccessTools.Method("TacticalGroups.ColonistGroup:DrawColonist");
			HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(Patch_Mod_LTOColonyGroupsFinal), nameof(Patch_Mod_LTOColonyGroupsFinal.Transpiler_ColonistGroup)));
			h.Patch(method, transpiler: transpiler);
			Log.Message("[Portraits] Patched TacticalGroups.ColonistGroup.DrawColonist");
		}
		private static IEnumerable<CodeInstruction> Transpiler_ColonistGroup(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			List<CodeInstruction> list = instructions.ToList();
			MethodInfo methodStart = AccessTools.Method("TacticalGroups.ColonistGroup:DrawCaravanSelectionOverlayOnGUI");
			MethodInfo methodEnd = AccessTools.Method(typeof(PortraitsCache), "Get");

			int index = list.FindIndex(x => x.Calls(methodStart)) + 1;
			int end = list.FindIndex(index, x => x.Calls(methodEnd)) + 1;
			if (index <= 0) {
				Log.Error("[Portraits] Failed to transpile TacticalGroups.ColonistGroup.DrawColonist: injection start index not found");
				return list;
			}
			if (end <= 0) {
				Log.Error("[Portraits] Failed to transpile TacticalGroups.ColonistGroup.DrawColonist: injection end index not found");
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

		private static void Patch_ColonistBarColonistDrawer(Harmony h) {
			MethodInfo method = AccessTools.Method("TacticalGroups.TacticalGroups_ColonistBarColonistDrawer:DrawColonist");
			HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(Patch_Mod_LTOColonyGroupsFinal), nameof(Patch_Mod_LTOColonyGroupsFinal.Transpiler_ColonistBarColonistDrawer)));
			h.Patch(method, transpiler: transpiler);
			Log.Message("[Portraits] Patched TacticalGroups...DrawColonist");
		}
		private static IEnumerable<CodeInstruction> Transpiler_ColonistBarColonistDrawer(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			List<CodeInstruction> list = new List<CodeInstruction>(instructions);
			FieldInfo Rot4South = AccessTools.Field("Verse.Rot4:South");
			MethodInfo PortraitsCacheGet = AccessTools.Method(typeof(PortraitsCache), "Get");

			int index = list.FindIndex(x => x.LoadsField(Rot4South)) - 3;
			int end = list.FindIndex(index, x => x.Calls(PortraitsCacheGet)) + 1;
			if (index <= 0) {
				Log.Error("[Portraits] Failed to transpile TacticalGroups.TacticalGroups_ColonistBarColonistDrawer.DrawColonist: injection start index not found");
				return list;
			}
			if (end <= 0) {
				Log.Error("[Portraits] Failed to transpile TacticalGroups.TacticalGroups_ColonistBarColonistDrawer.DrawColonist: injection end index not found");
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
