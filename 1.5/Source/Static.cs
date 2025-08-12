using Foxy.CustomPortraits.CustomPortraitsEx;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	[StaticConstructorOnStartup]
	public static class Static {
		public static readonly Texture2D atlasButtonBG = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBG");
		public static readonly Texture2D atlasButtonBGMouseover = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGMouseover");
		public static readonly Texture2D atlasButtonBGClick = ContentFinder<Texture2D>.Get("UI/Widgets/ButtonBGClick");
		public static readonly Texture2D texInspector = ContentFinder<Texture2D>.Get("UI/CustomPortraits/inspector");
		public static readonly Texture2D texStarOn = ContentFinder<Texture2D>.Get("UI/CustomPortraits/star_on");
		public static readonly Texture2D texStarOff = ContentFinder<Texture2D>.Get("UI/CustomPortraits/star_off");
		public static readonly Texture2D texFolder = ContentFinder<Texture2D>.Get("UI/CustomPortraits/folder");
		public static readonly Texture2D texFolderUp = ContentFinder<Texture2D>.Get("UI/CustomPortraits/up");
		public static readonly Texture2D texBlank = ContentFinder<Texture2D>.Get("UI/CustomPortraits/blank");

		static Static() {
            PortraitCache.Update();
            PortraitCacheEx.Update();

            Harmony h = new Harmony("Foxy.CustomPortraits");
			h.PatchAll();
			ModCompatibility.PatchAll(h);
		}
	}
}
