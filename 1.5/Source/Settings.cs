using RimWorld.Planet;
using UnityEngine;

namespace Foxy.CustomPortraits {
	public static class StaticSettings {
		private static global::CustomPortraits.Settings Instance => global::CustomPortraits.Settings.Instance;

		public static PortraitPosition Position => Instance.position;
		public static int CustomX => Instance.customX;
		public static int CustomY => Instance.customY;
		public static TextAnchor CustomAnchor => Instance.customAnchor;
		public static int SmallWidth => Instance.smallWidth;
		public static int SmallHeight => Instance.smallHeight;
		public static int BigWidth => Instance.bigWidth;
		public static int BigHeight => Instance.bigHeight;
		public static int OffsetX => Instance.offsetX;
		public static int OffsetY => Instance.offsetY;
		public static bool ShowWhenInspector => Instance.showWhenInspector;
		public static PortraitTabPosition TabPosition => Instance.tabPosition;
		public static bool ActionsBig => Instance.actionsBig;

		public static Vector2 BigSize => new Vector2(BigWidth, BigHeight);

		public static bool IsInspector => HasPosition(PortraitPosition.Inspector);
		public static bool IsColonistBar => HasPosition(PortraitPosition.ColonistBar);
		public static bool IsTopRight => HasPosition(PortraitPosition.TopRight);
		public static bool IsActions => HasPosition(PortraitPosition.Actions);
		public static bool IsCustom => HasPosition(PortraitPosition.Custom);

		public static bool HasPosition(PortraitPosition? position) {
			if (position.Value == PortraitPosition.ColonistBar && ModCompatibility.NalsCustomPortraits) return false;
			if (position.Value != PortraitPosition.ColonistBar && WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.Planet) return false;
			if (!position.HasValue) return true;
			return Position.HasFlag(position.Value);
		}
	}
}
