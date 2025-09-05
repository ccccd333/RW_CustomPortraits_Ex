using Foxy.CustomPortraits;
using UnityEngine;
using Verse;

// Old namespace - because scribe relies on full class names for settings
namespace CustomPortraits {
	public class Settings : ModSettings {
		private static Settings instance;
		public static Settings Instance {
			get {
				if (instance == null) instance = LoadedModManager.GetMod<ModMain>().Settings;
				return instance;
			}
		}

		public PortraitPosition position = PortraitPosition.Inspector;
		public int customX = 0;
		public int customY = 0;
		public TextAnchor customAnchor = TextAnchor.UpperLeft;
		public int smallWidth = 150;
		public int smallHeight = 150;
		public int bigWidth = 300;
		public int bigHeight = 300;
		public bool showWhenInspector = false;
		public PortraitTabPosition tabPosition = PortraitTabPosition.Bottom;
		public bool actionsBig = false;
		public int offsetX = 14;
		public int offsetY = 14;
		public bool debug = false;
		public bool advanced = false;

		public override void ExposeData() {
			Scribe_Values.Look(ref position, "position");
			Scribe_Values.Look(ref customX, "customX");
			Scribe_Values.Look(ref customY, "customY");
			Scribe_Values.Look(ref customAnchor, "customAnchor");
			Scribe_Values.Look(ref smallWidth, "smallWidth");
			Scribe_Values.Look(ref smallHeight, "smallHeight");
			Scribe_Values.Look(ref bigWidth, "bigWidth");
			Scribe_Values.Look(ref bigHeight, "bigHeight");
			Scribe_Values.Look(ref showWhenInspector, "showWhenInspector");
			Scribe_Values.Look(ref tabPosition, "tabPosition");
			Scribe_Values.Look(ref actionsBig, "actionsBig");
			Scribe_Values.Look(ref offsetX, "offsetX");
			Scribe_Values.Look(ref offsetY, "offsetY");
			Scribe_Values.Look(ref debug, "debug");
			Scribe_Values.Look(ref advanced, "advanced");
		}
	}
}
