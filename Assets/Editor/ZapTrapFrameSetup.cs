using UnityEditor;
using UnityEngine;

/// <summary>
/// Configures pre-cut ZapTrap frame PNGs so animation base stays aligned.
/// Ensures: Sprite (Single), Point filtering, alpha transparency, consistent PPU, and bottom-center pivot.
/// </summary>
public static class ZapTrapFrameSetup
{
    private const string BlueFramesDir = "Assets/Sprites/ZapTrapBlueFrames";
    private const string RedFramesDir  = "Assets/Sprites/ZapTrapRedFrames";

    [MenuItem("CS4483/⚡ Setup ZapTrap Blue Frames (Pre-cut)")]
    public static void SetupBlueFrames()
    {
        SetupFramesInDir(BlueFramesDir, "blue");
    }

    [MenuItem("CS4483/⚡ Setup ZapTrap Red Frames (Pre-cut)")]
    public static void SetupRedFrames()
    {
        SetupFramesInDir(RedFramesDir, "red");
    }

    private static void SetupFramesInDir(string dir, string label)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { dir });
        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"[ZapTrapFrameSetup] No {label} frames found in {dir}");
            return;
        }

        int changed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
            if (Mathf.Abs(importer.spritePixelsPerUnit - 32f) > 0.01f) { importer.spritePixelsPerUnit = 32f; dirty = true; }
            if (importer.alphaSource != TextureImporterAlphaSource.FromInput) { importer.alphaSource = TextureImporterAlphaSource.FromInput; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }

            // Force bottom-center pivot so the base of the trap stays planted.
            TextureImporterSettings tis = new TextureImporterSettings();
            importer.ReadTextureSettings(tis);
            if (tis.spriteAlignment != (int)SpriteAlignment.Custom) { tis.spriteAlignment = (int)SpriteAlignment.Custom; dirty = true; }
            if ((tis.spritePivot - new Vector2(0.5f, 0f)).sqrMagnitude > 0.0001f) { tis.spritePivot = new Vector2(0.5f, 0f); dirty = true; }
            importer.SetTextureSettings(tis);

            if (dirty)
            {
                importer.SaveAndReimport();
                changed++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"[ZapTrapFrameSetup] ✓ Configured {changed} zap trap {label} frame(s) with bottom pivot.");
    }
}

