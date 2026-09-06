using Foxy.CustomPortraits.CustomPortraitsEx;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	[HarmonyPatch(typeof(InspectPaneUtility), nameof(InspectPaneUtility.ExtraOnGUI))]
	public static class Patch_ExtraOnGUI {
		public static void Prefix(IInspectPane pane) {
			if (!pane.AnythingSelected) return;
			Pawn pawn = Helper.GetSelectedPawn();
			if (pawn is null) return;

			if (StaticSettings.IsInspector) {
				InspectTabBase tab = StaticSettings.ShowWhenInspector ? pane.GetOpenTab() : null;
				Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.Inspector);

				// インスペクターは有効にしててもキャラごとのポートレートを設定してない場合はなにもしない
				if (tex == null) {
                    ConditionDrivenPortrait.ResetAll();

                    return; 
				}

                string filename = pawn.GetPortraitName(PortraitPosition.Inspector);

                tex = ConditionDrivenPortrait.GetPortraitTexture(pawn, filename, tex);
                if (tex != null) {
                    PortraitDrawer.DrawNextToInspector(tex, pane.PaneTopY, tab);
                } else if (ConditionDrivenPortrait.IsVideoActive) {
                    CustomPortraitsEx.Repository.VideoPortraitDrawer.DrawNextToInspector(
                        ConditionDrivenPortrait.GetActiveVideoTexture(), pane.PaneTopY, tab);
                }
			}
			if (StaticSettings.IsTopRight) {
				Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.TopRight);
				if (tex != null) {
                    PortraitDrawer.DrawTopRight(tex);
                }
			}
			if (StaticSettings.IsCustom) {
				Texture2D tex = pawn.GetPortraitTexture(PortraitPosition.Custom);
				if (tex != null) {
                    PortraitDrawer.DrawCustom(tex);
                }
			}
		}
	}
}