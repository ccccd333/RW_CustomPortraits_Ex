using System;

namespace Foxy.CustomPortraits {
	[Flags]
	public enum PortraitPosition {
		Inspector   = 0b00001,
		ColonistBar = 0b00010,
		TopRight    = 0b00100,
		Actions     = 0b01000,
		Custom      = 0b10000
	}
}
