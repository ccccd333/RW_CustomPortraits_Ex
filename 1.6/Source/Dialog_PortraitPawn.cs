using System;
using System.Collections.Generic;
using System.IO;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class Dialog_PortraitPawn : Window {
		private readonly Pawn pawn;
		private readonly List<Widgets.DropdownMenuElement<string>> fileOptions = new List<Widgets.DropdownMenuElement<string>>();
		private readonly List<Widgets.DropdownMenuElement<PortraitPosition?>> positionOptions = new List<Widgets.DropdownMenuElement<PortraitPosition?>>();
		private PortraitPosition? position;
		private Vector2 scrollPosition = Vector2.zero;
		private DirectoryInfo CurrentDirectory { get; set; } = PortraitCache.Directory;
		private static readonly float itemHeight = 24;

		private static GUIStyle DirItemContainerStyle { get; } = new GUIStyle() {
			fixedHeight = itemHeight,
			normal = new GUIStyleState() { background = null },
			hover = new GUIStyleState() { background = TexUI.TextBGBlack }
		};
		private static GUIStyle DirItemSelectedContainerStyle { get; } = new GUIStyle() {
			fixedHeight = itemHeight,
			normal = new GUIStyleState() { background = TexUI.GrayBg },
			hover = new GUIStyleState() { background = TexUI.GrayTextBG }
		};
		private static GUIStyle DirItemIconStyle { get; } = new GUIStyle() {
			fixedHeight = itemHeight,
			fixedWidth = itemHeight - 4,
			margin = new RectOffset(0, 0, 4, 0),
			normal = new GUIStyleState() { background = null, textColor = Color.white }
		};
		private static GUIStyle DirItemTextStyle { get; } = new GUIStyle(Text.CurFontStyle) {
			fixedHeight = itemHeight,
			normal = new GUIStyleState() { background = null, textColor = Color.white },
			alignment = TextAnchor.MiddleLeft,
			wordWrap = false
		};

		public override Vector2 InitialSize => StaticSettings.BigSize + new Vector2(10f, 10f + 30f + 30f + 5f);
		protected override float Margin => 5f;

		private string SelectedFile => pawn.GetPortraitName(position);

		public Dialog_PortraitPawn(Pawn pawn) {
			optionalTitle = Helper.Label("PortraitFor", pawn);
			doCloseX = true;
			this.pawn = pawn;
			if (!string.IsNullOrEmpty(SelectedFile)) {
				CurrentDirectory = new FileInfo(Path.Combine(PortraitCache.Directory.FullName, SelectedFile)).Directory;
			}
			PortraitCache.Update();
			GeneratePositionOptions();
		}

		public static Dialog_PortraitPawn Open(Pawn pawn) {
			Dialog_PortraitPawn dialog = new Dialog_PortraitPawn(pawn);
			Find.WindowStack.Add(dialog);
			return dialog;
		}

		public override void DoWindowContents(Rect inRect) {
			if (WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.Planet) Close();
			inRect.SplitHorizontally(30, out Rect positionRect, out Rect fileRect);
			fileRect.yMin += 5f;
			Rect delRect = positionRect.RightPartPixels(30);
			positionRect.width -= 35f;

			Widgets.Dropdown(
				positionRect,
				this,
				d => d.position,
				(d) => d.positionOptions,
				buttonLabel: position.Translate()
			);
			try {
				GUILayout.BeginArea(fileRect);
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				DrawDir(CurrentDirectory);
				GUILayout.EndVertical();
				GUILayout.EndScrollView();
				GUILayout.EndArea();
			} catch (ArgumentException) {
				// Getting control 0's position in a group with only 0 controls when doing repaint
				// Aborting
			}

			if (pawn.HasPortraitName(position) && Widgets.ButtonImage(delRect, TexButton.Delete)) {
				pawn.SetPortraitName(position, null);
				UpdatePositionOptions();
			}

			if (Find.Selector.SingleSelectedThing is Pawn p && p != pawn) {
				Close();
				Dialog_PortraitPawn dialog = Open(p);
				dialog.position = position;
			}
		}

		private static bool DrawDirItem(Texture2D icon, string text, bool selected = false) {
			GUILayout.BeginHorizontal(selected ? DirItemSelectedContainerStyle : DirItemContainerStyle, GUILayout.ExpandWidth(true));
			GUILayout.Box(icon ?? Static.texBlank, DirItemIconStyle);
			bool cText = GUILayout.Button(text, DirItemTextStyle, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();
			return cText;
		}
		private void DrawDir(DirectoryInfo dir) {
			if (!dir.FullName.EqualsIgnoreCase(PortraitCache.Directory.FullName)) {
				if (DrawDirItem(Static.texFolderUp, "..")) {
					CurrentDirectory = CurrentDirectory.Parent;
				}
			}
			foreach (DirectoryInfo di in dir.EnumerateDirectories()) {
				if (DrawDirItem(Static.texFolder, di.Name)) {
					CurrentDirectory = di;
				}
			}
			foreach (FileInfo fi in dir.EnumerateFiles()) {
				GUI.enabled = PortraitCache.IsValidPortraitFile(fi);
				string filename = PortraitCache.GetRelativePath(fi);
				Texture2D portrait = PortraitCache.Get(filename);
				if (DrawDirItem(portrait, fi.Name, SelectedFile == filename)) {
					pawn.SetPortraitName(position, filename);
					UpdatePositionOptions();
				}
				GUI.enabled = true;
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
					mouseoverGuiAction = GetTooltipDelegate(Helper.Label("NalsTooltip"));
					enabled = false;
				} else if (pawn.RaceProps.Animal) {
					mouseoverGuiAction = GetTooltipDelegate(Helper.Label("AnimalTooltip"));
					enabled = false;
				} else if (!enabled) {
					mouseoverGuiAction = GetTooltipDelegate(Helper.Label("DisabledTooltip"));
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

		private static Action<Rect> GetTooltipDelegate(string text) {
			return (Rect rect) => TooltipHandler.TipRegion(rect, text);
		}

		private void UpdatePositionOptions() {
			foreach (var option in positionOptions) {
				Texture2D tex = pawn.HasPortraitName(option.payload) ? Static.texStarOn : Static.texStarOff;
				option.option.SetIcon(tex);
			}
		}
	}
}
