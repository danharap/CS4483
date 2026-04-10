using UnityEditor;
using UnityEngine;

/// <summary>
/// Trap frames under Assets/Resources/Traps must be imported as Sprites so
/// Resources.Load&lt;Sprite&gt; works at runtime. Copied PNGs default to Texture2D and
/// load as null — traps appear invisible.
/// </summary>
public static class ResourcesTrapSpriteImporter
{
    const string TrapsResourcesRoot = "Assets/Resources/Traps";

    [MenuItem("CS4483/🎨 Fix Resources trap textures (Sprite import)")]
    public static void ReimportAllTrapTexturesInResources()
    {
        if (!AssetDatabase.IsValidFolder(TrapsResourcesRoot))
        {
            Debug.LogWarning($"[ResourcesTrapSpriteImporter] Missing folder: {TrapsResourcesRoot}");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TrapsResourcesRoot });
        int n = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.spritePixelsPerUnit = 20f;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.crunchedCompression = false;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            n++;
        }

        Debug.Log($"[ResourcesTrapSpriteImporter] Reimported {n} trap texture(s) as Sprites under {TrapsResourcesRoot}.");
    }
}

/// <summary>
/// Auto-fixes any new PNG dropped under Resources/Traps.
/// </summary>
public sealed class ResourcesTrapTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string p = assetPath.Replace('\\', '/');
        if (!p.StartsWith("Assets/Resources/Traps/", System.StringComparison.OrdinalIgnoreCase)) return;
        if (!p.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) return;

        var ti = assetImporter as TextureImporter;
        if (ti == null) return;

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.spritePixelsPerUnit = 20f;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.crunchedCompression = false;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.alphaIsTransparency = true;
    }
}
