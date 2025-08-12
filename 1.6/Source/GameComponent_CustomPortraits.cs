using Foxy.CustomPortraits.CustomPortraitsEx;
using System.Collections.Generic;
using Verse;

namespace Foxy.CustomPortraits {
	public class GameComponent_CustomPortraits : GameComponent {
		public static GameComponent_CustomPortraits Instance { get; private set; }

		public Dictionary<string, PawnPortraits> animals = new Dictionary<string, PawnPortraits>();

		public GameComponent_CustomPortraits(Game game) : base() {
			Instance = this;
		}

        public override void StartedNewGame()
        {
            ConditionDrivenPortrait.Reset();
            Log.Message("[PortraitsEx] StartedNewGame.");
        }

        public override void LoadedGame()
        {
            ConditionDrivenPortrait.Reset();
            Log.Message("[PortraitsEx] LoadedGame.");
        }

        public override void ExposeData() {
			base.ExposeData();
			Scribe_Collections.Look(ref animals, "animals", valueLookMode: LookMode.Deep);
		}
	}
}
