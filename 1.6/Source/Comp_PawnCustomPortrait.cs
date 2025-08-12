using Foxy.CustomPortraits;
using Verse;

public class Comp_FoxyPawnCustomPortrait : ThingComp {
	private readonly PawnPortraits storage = new PawnPortraits();
	public PawnPortraits Storage => storage;

	public string this[PortraitPosition? position] {
		get => storage[position];
		set => storage[position] = value;
	}

	public bool HasFilename(PortraitPosition? position) {
		return storage.HasFilename(position);
	}

	public string GetFilename(PortraitPosition? position) {
		return storage.GetFilename(position);
	}

	public void SetFilename(PortraitPosition? position, string value) {
		storage.SetFilename(position, value);
	}

	public override void PostExposeData() {
		base.PostExposeData();
		storage.ExposeData();
	}
}
