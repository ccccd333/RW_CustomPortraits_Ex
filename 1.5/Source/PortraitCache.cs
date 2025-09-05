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
				Texture2D tex = new Texture2D(2, 2);
				try {
					if (file.Extension.ToLower() == ".dds") {
						tex.LoadImageDDS(data);
					} else {
						tex.LoadImage(data);
					}
					tex.name = path;
					cache.Add(path, tex);
				} catch (Exception ex) {
					Log.Error($"[Portraits] Portrait failed to load: {path}");
					Log.Error($"[Portraits] {ex.Message}");
					UnityEngine.Object.Destroy(tex);
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
	}
}
