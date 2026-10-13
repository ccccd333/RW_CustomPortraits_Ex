using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class Dialog_AdvancedPortraitPawn : Window {
		private readonly Pawn pawn;

		// Height from Dialog_SimplePortraitPawn, adjust in PreOpen later
		public override Vector2 InitialSize => new Vector2(500f, 375f);
		protected override float Margin => 5f;

		public Dialog_AdvancedPortraitPawn(Pawn pawn) {
			optionalTitle = Helper.Label("PortraitFor", pawn);
			doCloseX = true;
			this.pawn = pawn;
			PortraitCache.Update();
		}

		public override void PreOpen() {
			base.PreOpen();
			// Bottom edge aligned with the Simple dialog
			// Move top edge up to achieve target height
			windowRect.yMin -= 500f-InitialSize.y;
		}

		public static Dialog_AdvancedPortraitPawn Open(Pawn pawn) {
			Dialog_AdvancedPortraitPawn dialog = new Dialog_AdvancedPortraitPawn(pawn);
			Find.WindowStack.Add(dialog);
			return dialog;
		}

		public override void DoWindowContents(Rect inRect) {
			if (WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.Planet) Close();
			inRect.SplitHorizontally(30, out Rect positionRect, out Rect temp);
			temp.yMin += 5f;
			temp.SplitHorizontally(temp.height - 35, out Rect fileRect, out Rect advRect);
			Rect delRect = positionRect.RightPartPixels(30);
			positionRect.width -= 35f;

			bool advanced = true;
			Widgets.CheckboxLabeled(advRect, Helper.Label("AdvancedPortrait"), ref advanced);
			if (!advanced) {
				Close();
				StaticSettings.Advanced = false;
				Helper.OpenDialog(pawn);
			}

			if (Find.Selector.SingleSelectedThing is Pawn p && p != pawn) {
				Close();
				Open(p);
			}
		}
	}
}
