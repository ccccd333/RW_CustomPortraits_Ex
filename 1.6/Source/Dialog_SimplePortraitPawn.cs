using System;
using System.Collections.Generic;
using System.IO;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class Dialog_SimplePortraitPawn : Window {
		private readonly Pawn pawn;
		private readonly List<Widgets.DropdownMenuElement<PortraitPosition?>> positionOptions = new List<Widgets.DropdownMenuElement<PortraitPosition?>>();
		private PortraitPosition? position;
		private readonly GUI_FileDialog file_dialog = new GUI_FileDialog();

		public override Vector2 InitialSize => new Vector2(310f, 375f);
		protected override float Margin => 5f;

		private string SelectedFile => pawn.GetPortraitName(position);

		public Dialog_SimplePortraitPawn(Pawn pawn) {
			optionalTitle = Helper.Label("PortraitFor", pawn);
			doCloseX = true;
			this.pawn = pawn;
			if (!string.IsNullOrEmpty(SelectedFile)) {
				file_dialog.CurrentDirectory = new FileInfo(Path.Combine(PortraitCache.Directory.FullName, SelectedFile)).Directory;
			}
			PortraitCache.Update();
			GeneratePositionOptions();
		}

		public static Dialog_SimplePortraitPawn Open(Pawn pawn) {
			Dialog_SimplePortraitPawn dialog = new Dialog_SimplePortraitPawn(pawn);
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

			Widgets.Dropdown(
				positionRect,
				this,
				d => d.position,
				(d) => d.positionOptions,
				buttonLabel: position.Translate()
			);

			string filename = file_dialog.Draw(fileRect);
			if(filename != null) {
				pawn.SetPortraitName(position, filename);
				UpdatePositionOptions();
			}

			if (pawn.HasPortraitName(position) && Widgets.ButtonImage(delRect, TexButton.Delete)) {
				pawn.SetPortraitName(position, null);
				UpdatePositionOptions();
			}

#if DEBUG
			bool advanced = false;
			Widgets.CheckboxLabeled(advRect, Helper.Label("AdvancedPortrait"), ref advanced);
			if(advanced) {
				Close();
				StaticSettings.Advanced = true;
				Helper.OpenDialog(pawn);
			}
#endif

			if (Find.Selector.SingleSelectedThing is Pawn p && p != pawn) {
				Close();
				Dialog_SimplePortraitPawn dialog = Open(p);
				dialog.position = position;
			}
		}

		private void OnPositionSelection(PortraitPosition? position) {
			this.position = position;
		}

		private void GeneratePositionOptions() {
			positionOptions.Clear();

			Texture2D defaultTex = pawn.HasPortraitName(position) ? Static.texStarOn : Static.texStarOff;
			positionOptions.Add(new Widgets.DropdownMenuElement<PortraitPosition?>() {
				option = new FloatMenuOption(Extension.Translate(null), () => OnPositionSelection(null), defaultTex, Color.white),
				payload = null
			});
			foreach (PortraitPosition position in Enum.GetValues(typeof(PortraitPosition))) {
				bool enabled = StaticSettings.HasPosition(position);
				Texture2D tex = pawn.HasPortraitName(position) ? Static.texStarOn : Static.texStarOff;

				Action<Rect> mouseoverGuiAction = null;
				if (position == PortraitPosition.ColonistBar && ModCompatibility.NalsCustomPortraits) {
					mouseoverGuiAction = Helper.GetTooltipDelegate(Helper.Label("NalsTooltip"));
					enabled = false;
				} else if (pawn.RaceProps.Animal) {
					mouseoverGuiAction = Helper.GetTooltipDelegate(Helper.Label("AnimalTooltip"));
					enabled = false;
				} else if (!enabled) {
					mouseoverGuiAction = Helper.GetTooltipDelegate(Helper.Label("DisabledTooltip"));
				}

				positionOptions.Add(new Widgets.DropdownMenuElement<PortraitPosition?>() {
					option = new FloatMenuOption(
						position.Translate(),
						() => OnPositionSelection(position),
						tex,
						enabled ? Color.white : Color.gray,
						orderInPriority: enabled ? 0 : -10,
						mouseoverGuiAction: mouseoverGuiAction
					),
					payload = position
				});
			}
		}

		private void UpdatePositionOptions() {
			foreach (var option in positionOptions) {
				Texture2D tex = pawn.HasPortraitName(option.payload) ? Static.texStarOn : Static.texStarOff;
				option.option.SetIcon(tex);
			}
		}
	}
}
