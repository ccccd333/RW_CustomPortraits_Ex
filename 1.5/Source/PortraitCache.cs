using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;

namespace Foxy.CustomPortraits {
	public static class PortraitCache {
		private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
		private static readonly string[] extensions = new[] { ".png", ".jpg", ".jpeg", ".dds" };
		private static DirectoryInfo RimWorldRootDirectory { get; } = new DirectoryInfo(GenFilePaths.ModsFolderPath).Parent;
		public static DirectoryInfo Directory { get; } = RimWorldRootDirectory.CreateSubdirectory("CustomPortraits");

		public static IEnumerable<string> All => cache.Keys;

		public static Texture2D Get(string filename) {
			if (filename == null) return null;
			return cache.TryGetValue(filename, out Texture2D texture) ? texture : null;
		}
		public static bool Has(string filename) {
			if (filename == null) return false;
			return cache.ContainsKey(filename);
		}
		public static void Update() {
			Log.Message($"[Portraits] Updating cache from directory: {Directory.FullName}");
			if (!Directory.Exists) Directory.Create();
			ReadDirectory(Directory);
		}
		public static bool IsValidPortraitFile(FileInfo file) {
			return extensions.Contains(file.Extension.ToLower());
		}
		private static void ReadDirectory(DirectoryInfo directory) {
			foreach (FileInfo file in directory.EnumerateFiles().Where(IsValidPortraitFile)) {
				string path = GetRelativePath(file);
				if (Has(path)) continue;
				Log.Message($"[Portraits] New portrait: {path}");
				byte[] data = File.ReadAllBytes(file.FullName);
				Texture2D tex;
				try {
					if (file.Extension.ToLower() == ".dds") {
						tex = LoadTextureDDS(data);
					} else {
						tex = LoadTexture(data);
					}
					tex.name = path;
					cache.Add(path, tex);
				} catch (Exception ex) {
					Log.Error($"[Portraits] Portrait failed to load: {path}");
					Log.Error($"[Portraits] {ex.Message}");
					cache.Add(path, null);
				}
			}
			foreach (DirectoryInfo dir in directory.EnumerateDirectories()) {
				ReadDirectory(dir);
			}
		}
		public static void HardUpdate() {
			Log.Message($"[Portraits] Hard update is removing {cache.Count} portrait.");
			foreach (Texture2D tex in cache.Values) UnityEngine.Object.Destroy(tex);
			cache.Clear();
			Update();
		}
		public static string GetRelativePath(FileInfo fi) {
			return fi.FullName.Substring(Directory.FullName.Length + 1);
		}
		private static Texture2D LoadTexture(byte[] data) {
			Texture2D tex = new Texture2D(2, 2);
			tex.LoadImage(data);
			return tex;
		}
		private static Texture2D LoadTextureDDS(byte[] data) {
			DDS dds = DDS.Parse(data);

			TextureFormat fmt;
			if (dds.ddpfPixelFormat.IsDXT1) {
				fmt = TextureFormat.DXT1;
			} else if (dds.ddpfPixelFormat.IsDXT5) {
				fmt = TextureFormat.DXT5;
				if (dds.dwWidth % 4 != 0 || dds.dwHeight % 4 != 0) {
					throw new FormatException($"DXT5 format requires dimensions to be divisable by 4: {dds.dwWidth}x{dds.dwHeight}");
				}
			} else {
				// Not sure why I'm so pedant about exact wrong format, but I woke up after already writing this
				if (dds.ddpfPixelFormat.dwFlags.HasFlag(DDPF.FourCC)) {
					throw new FormatException($"Unsupported pixel format: {dds.ddpfPixelFormat.StringFourCC}");
				} else if (dds.ddpfPixelFormat.dwFlags.HasFlag(DDPF.RGB)) {
					// Thankfully I stopped myself before reporting in the exact A_R_G_B_ notation
					if (dds.ddpfPixelFormat.dwFlags.HasFlag(DDPF.AlphaPixels))
						throw new FormatException($"Unsupported pixel format: ARGB");
					else throw new FormatException($"Unsupported pixel format: RGB");
				} else {
					throw new FormatException($"Unsupported pixel format: unknown");
				}
			}

			// DDSD_LINEARSIZE is required for compressed formats and DXTn are all compressed
			if (!dds.dwFlags.HasFlag(DDSD.LinearSize))
				throw new FormatException($"Linear size flag not set for a compressed format (0x{(uint)dds.dwFlags:X8})");
			if (dds.dwFlags.HasFlag(DDSD.Pitch))
				throw new FormatException($"Pitch flag set for a compressed format (0x{(uint)dds.dwFlags:X8})");

			// Pixel data size should be equal to dwPitchOrLinearSize since DDSD_LINEARSIZE is required for DXT, but I can't bring myself to trust it.
			byte[] dxt = new byte[data.Length - dds.DataOffset];
			Buffer.BlockCopy(data, (int)dds.DataOffset, dxt, 0, data.Length - (int)dds.DataOffset);

			// Maybe this is important, I dunno.
			int mipMapCount = dds.dwFlags.HasFlag(DDSD.MipmapCount) && dds.dwMipMapCount > 1 ? (int)dds.dwMipMapCount : 1;

			Texture2D tex = new Texture2D((int)dds.dwWidth, (int)dds.dwHeight, fmt, mipMapCount, false);
			tex.LoadRawTextureData(dxt);
			tex.Apply();

			// DDS images are flipped which ends up being a lot of pain with DXT compression not supporting pixel operations.
			// Texture format has to be changed since RenderTexture doesn't support DXT.
			// And we need RenderTexture because Blit needs it. And I don't know how else to flip it but Blit.
			RenderTexture prev = RenderTexture.active;
			RenderTexture flip = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
			RenderTexture.active = flip;
			Graphics.Blit(tex, flip, new Vector2(1, -1), new Vector2(0, 1));

			Texture2D result = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, mipMapCount, false);
			result.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			result.Apply();
			RenderTexture.active = prev;

			// What's the point of using DDS if we convert it to RGB and lose memory/speed advantage of DXT?
			// This will compress ARGB32 back into DXT5 using Unity's own stuff!
			// And I'm fairly sure it will only do DXT5 and not DXT1 because there is an alpha channel in the flipped texture
			// …and there's no non-alpha pixel format in GPU (which RenderTexture is), so we are stuck with ARGB32.
			// There is RGB565, but it's supposedly old and bad.
			// Maybe Unity is smart enough to notice how there are no transparent pixels and choose DXT1, I dunno.
			result.Compress(false);

			UnityEngine.Object.Destroy(tex);
			UnityEngine.Object.Destroy(flip);

			return result;
		}
	}
}
