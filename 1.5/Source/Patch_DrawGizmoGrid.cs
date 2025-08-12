using HarmonyLib;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGrid))]
	public static class Patch_DrawGizmoGrid {
		public static void Prefix(ref float startX, ref Pawn __state) {
			if (StaticSettings.IsActions) {
				__state = Helper.GetSelectedPawn();
				if (__state != null && __state.GetPortraitTexture(PortraitPosition.Actions)) {
					startX += StaticSettings.OffsetX + GizmoGridDrawer.GizmoSpacing.x - GizmoGridDrawer.GizmoSpacing.y + StaticSettings.SmallWidth;
				}
			}
		}
		public static void Postfix(Pawn __state) {
			if (__state != null && __state.GetPortraitTexture(PortraitPosition.Actions)) {
				PortraitDrawer.DrawActions(__state.GetPortraitTexture(PortraitPosition.Actions));
			}
		}
	}
}
