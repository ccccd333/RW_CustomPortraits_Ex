using Verse;

namespace Foxy.CustomPortraits {
	public class PawnPortraits : IExposable {
		public string filename;
		public string inspector;
		public string colonistBar;
		public string topRight;
		public string actions;
		public string custom;

		public string this[PortraitPosition? position] {
			get => GetFilename(position);
			set => SetFilename(position, value);
		}

		public bool HasFilename(PortraitPosition? position) {
			if (!position.HasValue) return filename != null;
			switch (position.Value) {
				case PortraitPosition.Inspector: return inspector != null;
				case PortraitPosition.ColonistBar: return colonistBar != null;
				case PortraitPosition.TopRight: return topRight != null;
				case PortraitPosition.Actions: return actions != null;
				case PortraitPosition.Custom: return custom != null;
				default: return filename != null;
			}
		}
		public string GetFilename(PortraitPosition? position) {
			if (!position.HasValue) return filename;
			switch (position.Value) {
				case PortraitPosition.Inspector: return inspector ?? filename;
				case PortraitPosition.ColonistBar: return colonistBar ?? filename;
				case PortraitPosition.TopRight: return topRight ?? filename;
				case PortraitPosition.Actions: return actions ?? filename;
				case PortraitPosition.Custom: return custom ?? filename;
				default: return filename;
			}
		}
		public void SetFilename(PortraitPosition? position, string value) {
			if (!position.HasValue) {
				filename = value;
				return;
			}
			switch (position.Value) {
				case PortraitPosition.Inspector: {
						inspector = value;
						break;
					}
				case PortraitPosition.ColonistBar: {
						colonistBar = value;
						break;
					}
				case PortraitPosition.TopRight: {
						topRight = value;
						break;
					}
				case PortraitPosition.Actions: {
						actions = value;
						break;
					}
				case PortraitPosition.Custom: {
						custom = value;
						break;
					}
				default: {
						filename = value;
						break;
					}
			}
		}

		public void ExposeData() {
			Scribe_Values.Look(ref filename, "portrait");
			Scribe_Values.Look(ref inspector, "portrait_inspector");
			Scribe_Values.Look(ref colonistBar, "portrait_colonistBar");
			Scribe_Values.Look(ref topRight, "portrait_topRight");
			Scribe_Values.Look(ref actions, "portrait_actions");
			Scribe_Values.Look(ref custom, "portrait_custom");
		}
	}
}
