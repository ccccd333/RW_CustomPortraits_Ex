using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public static class Patch_Mod_ColorCodedMoodBar {
		public static void PatchAll(Harmony h) {
			Log.Message("[Portraits] Color Coded Mood Bar mod detected.");
			Patch_DrawColonist(h);
		}

		private static void Patch_DrawColonist(Harmony h) {
			MethodInfo method = AccessTools.Method("ColoredMoodBar13.MoodPatch:DrawColonist");
			HarmonyMethod transpiler = new HarmonyMethod(AccessTools.Method(typeof(Patch_Mod_ColorCodedMoodBar), nameof(Patch_Mod_ColorCodedMoodBar.Transpiler_DrawColonist)));
			h.Patch(method, transpiler: transpiler);
			Log.Message("[Portraits] Patched ColoredMoodBar13.MoodPatch.DrawColonist");
		}
		private static IEnumerable<CodeInstruction> Transpiler_DrawColonist(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			List<CodeInstruction> list = instructions.ToList();
			FieldInfo fieldStart = AccessTools.Field("ColoredMoodBar13.Main:cryptosleep");
			MethodInfo methodEnd = AccessTools.Method(typeof(PortraitsCache), "Get");

			int index = list.FindIndex(x => x.LoadsField(fieldStart));
			int end = list.FindIndex(index, x => x.Calls(methodEnd));
			if (index <= 0) {
				Log.Error("[Portraits] Failed to transpile TacticalGroups.ColonistGroup.DrawColonist: injection start index not found");
				return list;
			}
			if (end <= 0) {
				Log.Error("[Portraits] Failed to transpile TacticalGroups.ColonistGroup.DrawColonist: injection end index not found");
				return list;
			}

			index += 2;
			end += 1;
			Label labelSkip = il.DefineLabel();
			Label labelEnd = il.DefineLabel();
			CodeInstruction start = new CodeInstruction(OpCodes.Ldarg_2).MoveLabelsFrom(list[index]);
			list[end + 1].labels.Add(labelEnd);
			list[index].labels.Add(labelSkip);
			list.Insert(index++, start);
			list.Insert(index++, new CodeInstruction(OpCodes.Ldind_Ref));
			list.Insert(index++, new CodeInstruction(OpCodes.Call, Helper.ShouldDrawColonistBarMethod));
			list.Insert(index++, new CodeInstruction(OpCodes.Brfalse_S, labelSkip));
			list.Insert(index++, new CodeInstruction(OpCodes.Ldarg_1));
			list.Insert(index++, new CodeInstruction(OpCodes.Ldobj, typeof(Rect)));
			list.Insert(index++, new CodeInstruction(OpCodes.Ldarg_2));
			list.Insert(index++, new CodeInstruction(OpCodes.Ldind_Ref));
			list.Insert(index++, new CodeInstruction(OpCodes.Call, Helper.DrawColonistBarMethod));
			list.Insert(index++, new CodeInstruction(OpCodes.Br_S, labelEnd));
			return list;
		}
	}
}
