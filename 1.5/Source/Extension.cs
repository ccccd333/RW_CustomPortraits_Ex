using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public static class Extension {
		private static readonly FieldInfo fieldCachedPortraits = AccessTools.Field("RimWorld.PortraitsCache:cachedPortraits");
		private static readonly FieldInfo fieldItemIcon = AccessTools.Field("Verse.FloatMenuOption:itemIcon");
		private static readonly FieldInfo fieldIconColor = AccessTools.Field("Verse.FloatMenuOption:iconColor");
		private static readonly FieldInfo fieldIconJustification = AccessTools.Field("Verse.FloatMenuOption:iconJustification");
		private static readonly FieldInfo fieldExtraPartRightJustified = AccessTools.Field("Verse.FloatMenuOption:extraPartRightJustified");

		private static void RemoveCachedPortraits(Pawn pawn) {
			IDictionary cachedPortraits = (IDictionary)fieldCachedPortraits.GetValue(null);
			foreach (object value in cachedPortraits.Values) {
				IDictionary cache = (IDictionary)value;
				if (!cache.Contains(pawn)) continue;
				cache.Remove(pawn);
			}
		}

		public static void IntField(this Listing listing, string label, float labelWidth, float valueWidth, ref int value, ref string buffer) {
			Rect rect = listing.GetRect(Text.LineHeight);
			Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
			Rect valueRect = new Rect(rect.x + labelWidth, rect.y, valueWidth, rect.height);
			if (buffer == null) buffer = value.ToString();
			TextAnchor old = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(labelRect, label);
			Widgets.TextFieldNumeric(valueRect, ref value, ref buffer);
			Text.Anchor = old;
			listing.Gap(listing.verticalSpacing);
		}
		public static void ProperIndent(this Listing listing, float gapWidth = 12f) {
			listing.Indent(gapWidth);
			listing.ColumnWidth -= gapWidth;
		}
		public static void ProperOutdent(this Listing listing, float gapWidth = 12f) {
			listing.Outdent(gapWidth);
			listing.ColumnWidth += gapWidth;
		}

		public static void SetIcon(this FloatMenuOption option, Texture2D itemIcon, Color? iconColor = null, HorizontalJustification? iconJustification = null, bool? extraPartRightJustified = null) {
			fieldItemIcon.SetValue(option, itemIcon);
			if (iconColor.HasValue) fieldIconColor.SetValue(option, iconColor.Value);
			if (iconJustification.HasValue) fieldIconJustification.SetValue(option, iconJustification.Value);
			if (extraPartRightJustified.HasValue) fieldExtraPartRightJustified.SetValue(option, extraPartRightJustified.Value);
		}
		public static InspectTabBase GetOpenTab(this IInspectPane pane) {
			return pane?.CurTabs.FirstOrDefault(x => x.GetType() == pane.OpenTabType);
		}

		public static string Translate(this PortraitPosition position) {
			return Helper.Label($"Position.{position}");
		}
		public static string Translate(this PortraitPosition? position) {
			if (!position.HasValue) return Helper.Label("Position.Default");
			return position.Value.Translate();
		}

		[Obsolete("Use GetPortraitName(Pawn, PortraitPosition?) instead")]
		public static string GetPortraitName(this Pawn pawn) {
			return pawn.GetPortraitName(null);
		}
		[Obsolete("Use SetPortraitName(Pawn, PortraitPosition?, string) instead")]
		public static void SetPortraitName(this Pawn pawn, string filename) {
			pawn.SetPortraitName(null, filename);
		}
		[Obsolete("Use GetPortraitTexture(Pawn, PortraitPosition?) instead")]
		public static Texture2D GetPortraitTexture(this Pawn pawn) {
			return pawn.GetPortraitTexture(null);
		}

		public static bool HasPortraitName(this Pawn pawn, PortraitPosition? position) {
			if (pawn.RaceProps.Animal) {
				if (GameComponent_CustomPortraits.Instance == null) return false;
				if (!GameComponent_CustomPortraits.Instance.animals.TryGetValue(pawn.def.defName, out var pp)) {
					return false;
				}
				return pp.HasFilename(position);
			} else {
				return pawn.GetComp<Comp_FoxyPawnCustomPortrait>()?.HasFilename(position) ?? false;
			}
		}
		public static string GetPortraitName(this Pawn pawn, PortraitPosition? position) {
			if (pawn.RaceProps.Animal) {
				if (GameComponent_CustomPortraits.Instance == null) return null;
				if (!GameComponent_CustomPortraits.Instance.animals.TryGetValue(pawn.def.defName, out var pp)) {
					return null;
				}
				return pp.GetFilename(position);
			} else {
				return pawn.GetComp<Comp_FoxyPawnCustomPortrait>()?.GetFilename(position);
			}
		}
		public static void SetPortraitName(this Pawn pawn, PortraitPosition? position, string filename) {
			if (pawn.RaceProps.Animal) {
				if (GameComponent_CustomPortraits.Instance == null) return;
				if (!GameComponent_CustomPortraits.Instance.animals.TryGetValue(pawn.def.defName, out var pp)) {
					GameComponent_CustomPortraits.Instance.animals.Add(pawn.def.defName, pp = new PawnPortraits());
				}
				pp.SetFilename(position, filename);
			} else {
				pawn.GetComp<Comp_FoxyPawnCustomPortrait>()?.SetFilename(position, filename);
				RemoveCachedPortraits(pawn);
				ModCompatibility.OwlsColonistBarResetCache();
			}
		}
		public static Texture2D GetPortraitTexture(this Pawn pawn, PortraitPosition? position) {
			return PortraitCache.Get(pawn.GetPortraitName(position));
		}

		public static void LoadImageDDS(this Texture2D texture, byte[] data) {
			DDS dds = new DDS(data);
			dds.LoadIntoTexture(texture);
		}
	}
}
