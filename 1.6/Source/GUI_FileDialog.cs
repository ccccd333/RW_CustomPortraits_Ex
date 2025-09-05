using System;
using System.IO;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public class GUI_FileDialog {
		private Vector2 scrollPosition = Vector2.zero;
		public DirectoryInfo CurrentDirectory { get; set; } = PortraitCache.Directory;
		public string SelectedPath { get; set; } = "";

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

		public string Draw(Rect rect) {
			string clicked = null;
			try {
				GUILayout.BeginArea(rect);
				scrollPosition = GUILayout.BeginScrollView(scrollPosition);
				GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
				clicked = DrawDir(CurrentDirectory);
				GUILayout.EndVertical();
				GUILayout.EndScrollView();
				GUILayout.EndArea();
			} catch (ArgumentException) {
				// Getting control 0's position in a group with only 0 controls when doing repaint
				// Aborting
			}
			return clicked;
		}

		private static bool DrawDirItem(Texture2D icon, string text, bool selected = false) {
			GUILayout.BeginHorizontal(selected ? DirItemSelectedContainerStyle : DirItemContainerStyle, GUILayout.ExpandWidth(true));
			GUILayout.Box(icon ?? Static.texBlank, DirItemIconStyle);
			bool cText = GUILayout.Button(text, DirItemTextStyle, GUILayout.ExpandWidth(true));
			GUILayout.EndHorizontal();
			return cText;
		}
		private string DrawDir(DirectoryInfo dir) {
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
			string clicked = null;
			foreach (FileInfo fi in dir.EnumerateFiles()) {
				string filename = PortraitCache.GetRelativePath(fi);
				Texture2D portrait = PortraitCache.Get(filename);
				GUI.enabled = portrait != null;
				if (DrawDirItem(portrait, fi.Name, SelectedPath == filename)) {
					clicked = filename;
				}
				GUI.enabled = true;
			}
			return clicked;
		}
	}
}
