using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Foxy.CustomPortraits {
	// Partial DDS file structure from DirectX 9.0 documentation
	// Most of this is not really needed ingame since Unity only supports DXT1 and DXT5
	// …but I still like to at least validate the file since other formats can be found in the wild.
	// Did you know that ImageMagick defaults to DXT5? You can use `-define dds:compression=dxt1` to change that behavior. (TIL)
	public class DDS {
		#region File parsing
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

		public struct HeaderDDS {
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

			public static HeaderDDS Parse(byte[] data) {
				HeaderDDS dds = new HeaderDDS();
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
		#endregion

		public HeaderDDS Header { get; }
		public byte[] DXT { get; }
		public TextureFormat Format { get; }
		public int MipMapCount { get; }
		public int Width => (int)Header.dwWidth;
		public int Height => (int)Header.dwHeight;
		public bool IsDXT1 => Header.ddpfPixelFormat.IsDXT1;
		public bool IsDXT5 => Header.ddpfPixelFormat.IsDXT5;

		public DDS(byte[] data) {
			Header = HeaderDDS.Parse(data);

			if (IsDXT1) {
				Format = TextureFormat.DXT1;
			} else if (IsDXT5) {
				Format = TextureFormat.DXT5;
			} else {
				// Not sure why I'm so pedant about exact wrong format, but I woke up after already writing this
				if (Header.ddpfPixelFormat.dwFlags.HasFlag(DDPF.FourCC)) {
					throw new FormatException($"Unsupported pixel format: {Header.ddpfPixelFormat.StringFourCC}");
				} else if (Header.ddpfPixelFormat.dwFlags.HasFlag(DDPF.RGB)) {
					// Thankfully I stopped myself before reporting in the exact A_R_G_B_ notation
					if (Header.ddpfPixelFormat.dwFlags.HasFlag(DDPF.AlphaPixels))
						throw new FormatException($"Unsupported pixel format: ARGB");
					else throw new FormatException($"Unsupported pixel format: RGB");
				} else {
					throw new FormatException($"Unsupported pixel format: unknown");
				}
			}

			if (Header.dwWidth % 4 != 0 || Header.dwHeight % 4 != 0) {
				throw new FormatException($"DDS format requires dimensions to be divisable by 4: {Header.dwWidth}x{Header.dwHeight}");
			}

			// DDSD_LINEARSIZE is required for compressed formats and DXTn are all compressed
			if (!Header.dwFlags.HasFlag(DDSD.LinearSize))
				throw new FormatException($"Linear size flag not set for a compressed format (0x{(uint)Header.dwFlags:X8})");
			// DDSD_PITCH is the opposite of DDSD_LINEARSIZE, so it must not be set for compressed DXTn formats
			if (Header.dwFlags.HasFlag(DDSD.Pitch))
				throw new FormatException($"Pitch flag set for a compressed format (0x{(uint)Header.dwFlags:X8})");

			// Pixel data size should be equal to dwPitchOrLinearSize since DDSD_LINEARSIZE is required for DXT, but I can't bring myself to trust it.
			DXT = new byte[data.Length - Header.DataOffset];
			Buffer.BlockCopy(data, (int)Header.DataOffset, DXT, 0, data.Length - (int)Header.DataOffset);

			// Maybe this is important, I dunno.
			MipMapCount = Header.dwFlags.HasFlag(DDSD.MipmapCount) && Header.dwMipMapCount > 1 ? (int)Header.dwMipMapCount : 1;
		}

		public Texture2D CreateTexture() {
			Texture2D tex = new Texture2D(2, 2);
			LoadIntoTexture(tex);
			return tex;
		}

		public void LoadIntoTexture(Texture2D tex) {
			Texture2D temp = new Texture2D(Width, Height, Format, MipMapCount, false);
			temp.LoadRawTextureData(DXT);
			temp.Apply();

			// DDS images are flipped which ends up being a lot of pain with DXT compression not supporting pixel operations.
			// There is a bitwise flipping algorithm out there, but I couldn't find one stable enough to be less hassle than this.
			// Texture format has to be changed since RenderTexture doesn't support DXT.
			// And we need RenderTexture because Blit needs it. And I don't know how else to flip it but Blit.
			RenderTexture prev = RenderTexture.active;
			RenderTexture flip = new RenderTexture(temp.width, temp.height, 0, RenderTextureFormat.ARGB32);
			RenderTexture.active = flip;
			Graphics.Blit(temp, flip, new Vector2(1, -1), new Vector2(0, 1));

			tex.Resize(temp.width, temp.height, TextureFormat.ARGB32, false);
			tex.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
			tex.Apply();
			RenderTexture.active = prev;

			// What's the point of using DDS if we convert it to RGB and lose memory/speed advantage of DXT?
			// This will compress ARGB32 back into DXT5 using Unity's own stuff!
			// And I'm fairly sure it will only do DXT5 and not DXT1 because there is an alpha channel in the flipped texture
			// …and there's no non-alpha pixel format in GPU (which RenderTexture is), so we are stuck with ARGB32.
			// There is RGB565, but it's supposedly old and bad.
			// Maybe Unity is smart enough to notice how there are no transparent pixels and choose DXT1, I dunno.
			// Not sure how much difference that makes either way.
			tex.Compress(false);

			UnityEngine.Object.Destroy(temp);
			UnityEngine.Object.Destroy(flip);
		}
	}
}
