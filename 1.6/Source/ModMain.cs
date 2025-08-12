using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Settings = CustomPortraits.Settings;

namespace Foxy.CustomPortraits {
	[StaticConstructorOnStartup]
	public class ModMain : Mod {
		public Settings Settings => GetSettings<Settings>();
		private string bufferCustomX;
		private string bufferCustomY;
		private string bufferSmallWidth;
		private string bufferSmallHeight;
		private string bufferBigWidth;
		private string bufferBigHeight;
		private string bufferOffsetX;
		private string bufferOffsetY;
		private float labelWidth;
		private Vector2 scroll;
		private float scrollHeight = 0;

		public ModMain(ModContentPack content) : base(content) { }

		public override string SettingsCategory() {
			return Content.Name;
		}
		public override void DoSettingsWindowContents(Rect inRect) {
			Rect viewRect = new Rect(0, 0, inRect.width - 17f, scrollHeight);
			Widgets.BeginScrollView(inRect, ref scroll, viewRect);
			Listing_Standard listing = new Listing_Standard(inRect.AtZero(), () => scroll) {
				maxOneColumn = true,
				ColumnWidth = viewRect.width
			};
			listing.Begin(viewRect);

			Text.Font = GameFont.Small;
			Settings settings = Settings;

			if (labelWidth < 1f) {
				labelWidth = new[] {
					"CustomX", "CustomY",
					"OffsetX", "OffsetY",
					"Small.Width", "Small.Height",
					"Big.Width", "Big.Height"
				}.Max(x => Text.CalcSize(Helper.Label(x) + " ").x);
			}

			Rect topRect = listing.Label(Helper.Label("Position"));
			Rect buttonRect = topRect.RightPart(0.3f);

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				if (Widgets.ButtonText(buttonRect, Helper.Label("OpenFolder"))) {
					if (!PortraitCache.Directory.Exists) PortraitCache.Directory.Create();
					SoundDefOf.Click.PlayOneShotOnCamera();
					ProcessStartInfo start = new ProcessStartInfo("explorer.exe", $"\"{PortraitCache.Directory.FullName}\"");
					Process.Start(start);
				}
				string[] split = PortraitCache.Directory.FullName.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				string target = PortraitCache.Directory.Parent.Name.ToLower();
				int i = 0;
				for (; i < split.Length; ++i) {
					if (split[i].ToLower() == target) break;
				}
				string path = "..." + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar.ToString(), split.Skip(i));
				TooltipHandler.TipRegion(buttonRect, Helper.Label("OpenFolder.Tooltip", path));
				buttonRect.x -= buttonRect.width + 5f;
			}

			if (Widgets.ButtonText(buttonRect, Helper.Label("UpdateCache"))) {
				PortraitCache.HardUpdate();
			}
			TooltipHandler.TipRegion(buttonRect, Helper.Label("UpdateCache.Tooltip"));

			listing.GapLine();

			foreach (PortraitPosition pos in Enum.GetValues(typeof(PortraitPosition))) {
				bool value = settings.position.HasFlag(pos);
				listing.CheckboxLabeled(pos.Translate(), ref value, tooltip: Helper.Label($"Position.{pos}.Tooltip"));
				if (value) settings.position |= pos;
				else settings.position &= ~pos;
			}

			listing.GapLine();

			if (settings.position.HasFlag(PortraitPosition.Inspector)) {
				listing.Label(PortraitPosition.Inspector.Translate());
				listing.ProperIndent();
				listing.CheckboxLabeled(Helper.Label("ShowWhenInspector"), ref settings.showWhenInspector, Helper.Label("ShowWhenInspector.Tooltip"));

				if (settings.showWhenInspector) {
					Rect rect = listing.GetRect(Static.texInspector.height);
					rect.x += labelWidth;
					rect.width = Static.texInspector.width;
					GUI.DrawTexture(rect, Static.texInspector);

					Rect left = new Rect(rect.x, rect.y, 30, 30);
					Rect top = new Rect(rect.xMax - 60, rect.y, 30, 30);
					Rect right = new Rect(rect.xMax - 30, rect.y + 30, 30, 30);
					Rect bottom = new Rect(rect.xMax - 30, rect.yMax - 80, 30, 30);
					if (ToggleButton(left, settings.tabPosition == PortraitTabPosition.Left)) settings.tabPosition = PortraitTabPosition.Left;
					if (ToggleButton(top, settings.tabPosition == PortraitTabPosition.Top)) settings.tabPosition = PortraitTabPosition.Top;
					if (ToggleButton(right, settings.tabPosition == PortraitTabPosition.Right)) settings.tabPosition = PortraitTabPosition.Right;
					if (ToggleButton(bottom, settings.tabPosition == PortraitTabPosition.Bottom)) settings.tabPosition = PortraitTabPosition.Bottom;
				}
				listing.ProperOutdent();
				listing.GapLine();
			}

			if (settings.position.HasFlag(PortraitPosition.Actions)) {
				listing.Label(PortraitPosition.Actions.Translate());
				listing.ProperIndent();
				listing.IntField(Helper.Label("OffsetX"), labelWidth, 200f, ref settings.offsetX, ref bufferOffsetX);
				listing.IntField(Helper.Label("OffsetY"), labelWidth, 200f, ref settings.offsetY, ref bufferOffsetY);
				listing.CheckboxLabeled(Helper.Label("ActionsBig.Label"), ref settings.actionsBig, Helper.Label("ActionsBig.Tooltip"));
				listing.ProperOutdent();
				listing.GapLine();
			}

			if (settings.position.HasFlag(PortraitPosition.Custom)) {
				listing.Label(PortraitPosition.Custom.Translate());
				listing.ProperIndent();
				listing.IntField(Helper.Label("CustomX"), labelWidth, 200f, ref settings.customX, ref bufferCustomX);
				listing.IntField(Helper.Label("CustomY"), labelWidth, 200f, ref settings.customY, ref bufferCustomY);
				Rect rect = listing.GetRect(150);
				rect.x += labelWidth;
				rect.width = 200f;
				Rect[] split = Split9(rect);
				if (ToggleButton(split[0], settings.customAnchor == TextAnchor.UpperLeft)) settings.customAnchor = TextAnchor.UpperLeft;
				if (ToggleButton(split[1], settings.customAnchor == TextAnchor.UpperCenter)) settings.customAnchor = TextAnchor.UpperCenter;
				if (ToggleButton(split[2], settings.customAnchor == TextAnchor.UpperRight)) settings.customAnchor = TextAnchor.UpperRight;
				if (ToggleButton(split[3], settings.customAnchor == TextAnchor.MiddleLeft)) settings.customAnchor = TextAnchor.MiddleLeft;
				if (ToggleButton(split[4], settings.customAnchor == TextAnchor.MiddleCenter)) settings.customAnchor = TextAnchor.MiddleCenter;
				if (ToggleButton(split[5], settings.customAnchor == TextAnchor.MiddleRight)) settings.customAnchor = TextAnchor.MiddleRight;
				if (ToggleButton(split[6], settings.customAnchor == TextAnchor.LowerLeft)) settings.customAnchor = TextAnchor.LowerLeft;
				if (ToggleButton(split[7], settings.customAnchor == TextAnchor.LowerCenter)) settings.customAnchor = TextAnchor.LowerCenter;
				if (ToggleButton(split[8], settings.customAnchor == TextAnchor.LowerRight)) settings.customAnchor = TextAnchor.LowerRight;
				listing.Gap(listing.verticalSpacing);
				listing.ProperOutdent();
				listing.GapLine();
			}

			listing.Label(Helper.Label("Small.Label"));
			listing.ProperIndent();
			listing.IntField(Helper.Label("Small.Width"), labelWidth, 200f, ref settings.smallWidth, ref bufferSmallWidth);
			listing.IntField(Helper.Label("Small.Height"), labelWidth, 200f, ref settings.smallHeight, ref bufferSmallHeight);
			listing.ProperOutdent();
			listing.Gap();

			PortraitPosition withoutColonistBar = settings.position & ~PortraitPosition.ColonistBar;
			bool onlyActions = (withoutColonistBar & ~PortraitPosition.Actions) == 0;

			if ((settings.position.HasFlag(PortraitPosition.Actions) && settings.actionsBig) || !onlyActions) {
				listing.Label(Helper.Label("Big.Label"));
				listing.ProperIndent();
				listing.IntField(Helper.Label("Big.Width"), labelWidth, 200f, ref settings.bigWidth, ref bufferBigWidth);
				listing.IntField(Helper.Label("Big.Height"), labelWidth, 200f, ref settings.bigHeight, ref bufferBigHeight);
				listing.ProperOutdent();
			}

			listing.GapLine();
			listing.ProperIndent();
			listing.CheckboxLabeled(Helper.Label("Dev.Label"), ref settings.debug, tooltip: Helper.Label("Dev.Tooltip"));
			listing.ProperOutdent();
			listing.Gap();


			if (Event.current.type == EventType.Layout) {
				scrollHeight = listing.CurHeight;
			}

			listing.End();
			Widgets.EndScrollView();
		}
		private bool ToggleButton(Rect rect, bool active) {
			Texture2D atlas = Static.atlasButtonBG;
			if (Mouse.IsOver(rect)) atlas = Static.atlasButtonBGMouseover;
			if (active) atlas = Static.atlasButtonBGClick;
			Widgets.DrawAtlas(rect, atlas);
			if (Widgets.ButtonInvisible(rect)) {
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
				return true;
			}
			return false;
		}
		private static Rect[] Split9(Rect rect) {
			Rect[] split = new Rect[9];
			float w = rect.width / 3;
			float h = rect.height / 3;

			split[0] = new Rect(rect.x, rect.y, w, h); // UpperLeft
			split[1] = new Rect(rect.x + w, rect.y, w, h); // UpperCenter
			split[2] = new Rect(rect.x + w + w, rect.y, w, h); // UpperRight

			split[3] = new Rect(rect.x, rect.y + h, w, h); // MiddleLeft
			split[4] = new Rect(rect.x + w, rect.y + h, w, h); // MiddleCenter
			split[5] = new Rect(rect.x + w + w, rect.y + h, w, h); // MiddleRight

			split[6] = new Rect(rect.x, rect.y + h + h, w, h); // LowerLeft
			split[7] = new Rect(rect.x + w, rect.y + h + h, w, h); // LowerCenter
			split[8] = new Rect(rect.x + w + w, rect.y + h + h, w, h); // LowerRight

			return split;
		}
	}
}
