using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits.CustomPortraitsEx.Repository {
	/// <summary>
	/// 後方互換ラッパー。実描画とクリック貫通防止は PortraitDrawer に統一。
	/// </summary>
	public static class VideoPortraitDrawer {
		public static void DrawNextToInspector(Texture tex, float paneTopY, InspectTabBase tab) =>
			PortraitDrawer.DrawNextToInspector(tex, paneTopY, tab);

		public static void DrawTopRight(Texture tex) =>
			PortraitDrawer.DrawTopRight(tex);

		public static void DrawActions(Texture tex) =>
			PortraitDrawer.DrawActions(tex);

		public static void DrawCustom(Texture tex) =>
			PortraitDrawer.DrawCustom(tex);

		public static void DrawColonistBar(Rect rect, Texture tex) =>
			PortraitDrawer.DrawColonistBar(rect, tex);
	}
}
