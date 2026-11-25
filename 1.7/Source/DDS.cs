using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Foxy.CustomPortraits {
	// Partial DDS file structure from DirectX 9.0 documentation
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
			public uint dwPitchOrLinearSize;
			public uint dwDepth;
			public uint dwMipMapCount;
			public DDS_PixelFormat ddpfPixelFormat;

			public uint DataOffset => 4 + dwSize; // 4 for magic; == 0x80 (128)

			public static HeaderDDS Parse(byte[] data) {
				if (data == null || data.Length < 128)
					throw new FormatException($"Invalid DDS file length: {data?.Length ?? 0}");

				ReadOnlySpan<byte> span = data;
				HeaderDDS dds = new HeaderDDS();
				dds.dwMagic = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(0, 4));
				if (dds.dwMagic != 0x20534444) throw new FormatException($"Invalid DDS magic: 0x{dds.dwMagic:X8}");
				dds.dwSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4));
				if (dds.dwSize != 0x0000007C) throw new FormatException($"Invalid DDS header size: 0x{dds.dwSize:X8}");
				dds.dwFlags = (DDSD)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8, 4));
				if (!dds.dwFlags.HasFlag(DDSD.Caps)) throw new FormatException($"Invalid DDS flag, no DDSD_CAPS: 0x{dds.dwFlags:X8}");
				if (!dds.dwFlags.HasFlag(DDSD.PixelFormat)) throw new FormatException($"Invalid DDS flag, no DDSD_PIXELFORMAT: 0x{dds.dwFlags:X8}");
				if (!dds.dwFlags.HasFlag(DDSD.Width)) throw new FormatException($"Invalid DDS flag, no DDSD_WIDTH: 0x{dds.dwFlags:X8}");
				if (!dds.dwFlags.HasFlag(DDSD.Height)) throw new FormatException($"Invalid DDS flag, no DDSD_HEIGHT: 0x{dds.dwFlags:X8}");
				dds.dwHeight = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12, 4));
				dds.dwWidth = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(16, 4));
				dds.dwPitchOrLinearSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(20, 4));
				dds.dwDepth = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(24, 4));
				dds.dwMipMapCount = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(28, 4));
				// Skip 11 reserved DWORDs (bytes 32..75)
				dds.ddpfPixelFormat.dwSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(76, 4));
				if (dds.ddpfPixelFormat.dwSize != 0x00000020) throw new FormatException($"Invalid DDS pixel format size: 0x{dds.ddpfPixelFormat.dwSize:X8}");
				dds.ddpfPixelFormat.dwFlags = (DDPF)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(80, 4));
				dds.ddpfPixelFormat.dwFourCC = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(84, 4));
				return dds;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DDS_PixelFormat {
			public uint dwSize;
			[MarshalAs(UnmanagedType.U4)]
			public DDPF dwFlags;
			public uint dwFourCC;

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
				if (Header.ddpfPixelFormat.dwFlags.HasFlag(DDPF.FourCC)) {
					throw new FormatException($"Unsupported pixel format: {Header.ddpfPixelFormat.StringFourCC}");
				} else if (Header.ddpfPixelFormat.dwFlags.HasFlag(DDPF.RGB)) {
					if (Header.ddpfPixelFormat.dwFlags.HasFlag(DDPF.AlphaPixels))
						throw new FormatException("Unsupported pixel format: ARGB");
					else throw new FormatException("Unsupported pixel format: RGB");
				} else {
					throw new FormatException("Unsupported pixel format: unknown");
				}
			}

			if (Header.dwWidth == 0 || Header.dwHeight == 0 || Header.dwWidth % 4 != 0 || Header.dwHeight % 4 != 0) {
				throw new FormatException($"DDS format requires dimensions to be divisable by 4: {Header.dwWidth}x{Header.dwHeight}");
			}

			if (!Header.dwFlags.HasFlag(DDSD.LinearSize))
				throw new FormatException($"Linear size flag not set for a compressed format (0x{(uint)Header.dwFlags:X8})");
			if (Header.dwFlags.HasFlag(DDSD.Pitch))
				throw new FormatException($"Pitch flag set for a compressed format (0x{(uint)Header.dwFlags:X8})");

			int blockSize = IsDXT1 ? 8 : 16;
			int mip0Size = (Width / 4) * (Height / 4) * blockSize;
			int available = data.Length - (int)Header.DataOffset;
			if (available < mip0Size) {
				throw new FormatException($"DDS payload truncated: expected at least {mip0Size} bytes, got {available}");
			}

			// Mip0 のみをコピー（不要な Mip1 以降の確保・反転を回避）
			DXT = new byte[mip0Size];
			Buffer.BlockCopy(data, (int)Header.DataOffset, DXT, 0, mip0Size);

			MipMapCount = 1;
		}

		public Texture2D CreateTexture() {
			if (IsDXT1)
				CustomPortraitsEx.Utility.FlipDXT1(DXT, Width, Height);
			else
				CustomPortraitsEx.Utility.FlipDXT5(DXT, Width, Height);

			Texture2D tex = new Texture2D(Width, Height, Format, mipCount: 1, linear: false, createUninitialized: true);
			tex.LoadRawTextureData(DXT);
			tex.Apply(false, true);
			return tex;
		}

		public void LoadIntoTexture(Texture2D tex) {
			if (IsDXT1)
				CustomPortraitsEx.Utility.FlipDXT1(DXT, Width, Height);
			else
				CustomPortraitsEx.Utility.FlipDXT5(DXT, Width, Height);

			Texture2D temp = new Texture2D(Width, Height, Format, mipCount: 1, linear: false, createUninitialized: true);
			RenderTexture rt = null;
			RenderTexture prev = RenderTexture.active;
			try {
				temp.LoadRawTextureData(DXT);
				temp.Apply(false, true);

				rt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32);
				RenderTexture.active = rt;
				Graphics.Blit(temp, rt);

				tex.Reinitialize(Width, Height, TextureFormat.ARGB32, false);
				tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
				tex.Apply();
				tex.Compress(false);
			} finally {
				RenderTexture.active = prev;
				UnityEngine.Object.Destroy(temp);
				if (rt != null) UnityEngine.Object.Destroy(rt);
			}
		}
	}
}
