using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Blackhole.EditorTools
{
    /// <summary>
    /// Tools/ArtGen 이 만든 PNG 를 art_manifest.json 에 적힌 값(PPU, 9-slice 테두리, 밉맵, 반복)대로 가져온다.
    /// 그림을 다시 뽑아도 설정을 손으로 고칠 필요가 없다.
    /// </summary>
    public class ArtImportPostprocessor : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/_Game/Art/";
        const string ManifestPath = ArtRoot + "art_manifest.json";

        [Serializable]
        class Entry
        {
            public string path;
            public int w, h;
            public float ppu = 100;
            public string kind;
            public bool mip;
            public string wrap;
            public float[] border;
        }

        [Serializable]
        class Manifest
        {
            public Entry[] items;
        }

        static Dictionary<string, Entry> s_Entries;
        static DateTime s_LoadedAt;

        static Entry Find(string relativePath)
        {
            if (!File.Exists(ManifestPath)) return null;
            var stamp = File.GetLastWriteTimeUtc(ManifestPath);
            if (s_Entries == null || stamp != s_LoadedAt)
            {
                s_Entries = new Dictionary<string, Entry>();
                var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
                foreach (var e in manifest.items) s_Entries[e.path] = e;
                s_LoadedAt = stamp;
            }
            return s_Entries.TryGetValue(relativePath, out var entry) ? entry : null;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal)) return;
            var entry = Find(assetPath.Substring(ArtRoot.Length));
            if (entry == null) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = entry.ppu;
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.mipmapEnabled = entry.mip;
            ti.filterMode = entry.mip ? FilterMode.Trilinear : FilterMode.Bilinear;
            ti.wrapMode = entry.wrap == "repeat" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(settings);

            ti.spriteBorder = entry.border != null && entry.border.Length == 4
                ? new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3])
                : Vector4.zero;
        }
    }
}
