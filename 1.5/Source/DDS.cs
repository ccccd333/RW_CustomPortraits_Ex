using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Foxy.CustomPortraits {
	// Partial DDS file structure from DirectX 9.0 documentation
	// Most of this is not really needed ingame since Unity only supports DXT1 and DXT5
	// …but I still like to at least validate the file since other formats can be found in the wild.
	// Did you know that ImageMagick defaults to DXT5? You can use `-define dds:compression=dxt1` to change that behavior. (TIL)
	[Flags]
	public enum DDSD : uint {
		Caps = 0x00000001,
		Height = 0x00000002,
		Width = 0x00000004,
		Pitch = 0x00000008,
		PixelFormat = 0x00001000,
		MipmapCount = 0x00020000,
		LinearSize = 0x00080000,
		Depth = 0x00800000
	}
	[Flags]
	public enum DDPF : uint {
		AlphaPixels = 0x00000001,
		FourCC = 0x00000004,
		RGB = 0x00000040,
	}

	public struct DDS {
		// Everything is LittleEndian
		public uint dwMagic; // Must be 0x20534444 (== "DDS " in ASCII LE)
		public uint dwSize; // Must be 0x0000007C (124)
		public DDSD dwFlags; // DDSD flags; Must match DDSD.Caps | DDSD.PixelFormat | DDSD.Width | DDSD.Height
		public uint dwHeight; // In pixels
		public uint dwWidth; // In pixels
		public uint dwPitchOrLinearSize; // If has flag DDSD.Pitch, then bytes per line; otherwise must have flag DDSD.LinearSize, then total amount of bytes
		public uint dwDepth; // For volume textures, must have flag DDSD.Depth
		public uint dwMipMapCount; // For images with mipmap levels, must have flag DDSD.MipmapCount;
		public DDS_PixelFormat ddpfPixelFormat;
		// There's more stuff after this, but we don't need that.

		public uint DataOffset => 4 + dwSize; // 4 for magic; == 0x80 (128)

		public static DDS Parse(byte[] data) {
			DDS dds = new DDS();
			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader br = new BinaryReader(ms)) {
				dds.dwMagic = br.ReadUInt32();
				if (dds.dwMagic != 0x20534444) throw new FormatException($"Invalid DDS magic: 0x{dds.dwMagic:X8}");
				dds.dwSize = br.ReadUInt32();
				if (dds.dwSize != 0x0000007C) throw new FormatException($"Invalid DDS header size: 0x{dds.dwSize:X8}");
				dds.dwFlags = (DDSD)br.ReadUInt32();
				if (!dds.dwFlags.HasFlag(DDSD.Caps)) throw new FormatException($"Invalid DDS flag, no DDSD_CAPS: 0x{dds.dwFlags:X8}");
				if (!dds.dwFlags.HasFlag(DDSD.PixelFormat)) throw new FormatException($"Invalid DDS flag, no DDSD_PIXELFORMAT: 0x{dds.dwFlags:X8}");
				if (!dds.dwFlags.HasFlag(DDSD.Width)) throw new FormatException($"Invalid DDS flag, no DDSD_WIDTH: 0x{dds.dwFlags:X8}");
				if (!dds.dwFlags.HasFlag(DDSD.Height)) throw new FormatException($"Invalid DDS flag, no DDSD_HEIGHT: 0x{dds.dwFlags:X8}");
				dds.dwHeight = br.ReadUInt32();
				dds.dwWidth = br.ReadUInt32();
				dds.dwPitchOrLinearSize = br.ReadUInt32();
				dds.dwDepth = br.ReadUInt32();
				dds.dwMipMapCount = br.ReadUInt32();
				br.ReadBytes(4 * 11); // 11 dword reserved
				dds.ddpfPixelFormat.dwSize = br.ReadUInt32();
				if (dds.ddpfPixelFormat.dwSize != 0x00000020) throw new FormatException($"Invalid DDS pixel format size: 0x{dds.ddpfPixelFormat.dwSize:X8}");
				dds.ddpfPixelFormat.dwFlags = (DDPF)br.ReadUInt32();
				dds.ddpfPixelFormat.dwFourCC = br.ReadUInt32();
				return dds;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DDS_PixelFormat {
		public uint dwSize; // Must be 0x00000020 (32)
		[MarshalAs(UnmanagedType.U4)]
		public DDPF dwFlags; // DDPF flags
		public uint dwFourCC; // Four-character code for format, must have flag DDPF.FourCC
							  // There's more RGB stuff, but we don't need that.

		public bool IsDXT1 => dwFlags.HasFlag(DDPF.FourCC) && dwFourCC == 0x31545844; // == "DXT1" in ASCII LE
		public bool IsDXT5 => dwFlags.HasFlag(DDPF.FourCC) && dwFourCC == 0x35545844; // == "DXT5" in ASCII LE
		public string StringFourCC => System.Text.Encoding.ASCII.GetString(BitConverter.GetBytes(dwFourCC));
	}
}
