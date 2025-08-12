using System.Linq;
using HarmonyLib;
using Verse;

namespace Foxy.CustomPortraits {
	public static class ModCompatibility {
		public static bool IsModActive(string packageId) {
			packageId = packageId.ToLower();
			return ModLister.AllInstalledMods.Any(x => x.Active && x.packageIdLowerCase == packageId);
		}

		public static bool LTOColonyGroupsFinal => IsModActive("DerekBickley.LTOColonyGroupsFinal");
		public static bool ColorCodedMoodBar => IsModActive("CrashM.ColorCodedMoodBar.11");
		public static bool OwlsColonistBar => IsModActive("Owlchemist.OwlsColonistBar");
		public static bool NalsCustomPortraits { get; private set; }

		public static void PatchAll(Harmony h) {
			if (LTOColonyGroupsFinal) Patch_Mod_LTOColonyGroupsFinal.PatchAll(h);
			if (ColorCodedMoodBar) Patch_Mod_ColorCodedMoodBar.PatchAll(h);
			if (OwlsColonistBar) Patch_Mod_OwlsColonistBar.PatchAll(h);
			if (IsModActive("Nals.CustomPortraits")) NalsCustomPortraits = true;
		}

		public static void OwlsColonistBarResetCache() {
			if (OwlsColonistBar) Patch_Mod_OwlsColonistBar.ResetCache();
		}
	}
}
