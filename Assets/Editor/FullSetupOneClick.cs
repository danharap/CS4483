using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click workflow for rapid iteration.
/// Runs the full pipeline in the correct order so you don't have to click 6+ menu items.
/// </summary>
public static class FullSetupOneClick
{
    [MenuItem("CS4483/⚡ FULL SETUP (One Click)")]
    public static void RunFullSetup()
    {
        Debug.Log("[FullSetupOneClick] Starting FULL SETUP…");

        try
        {
            // 0) Resize high-res pickup art (safe to run repeatedly)
            ImageResizer.ResizePickupSprites();

            // 1) Ensure sprite import settings / slicing are correct
            SpriteSetup.SliceSpriteSheets();

            // 1.5) Ensure pre-cut zap trap frames have correct pivot/settings
            ZapTrapFrameSetup.SetupBlueFrames();

            // 2) Build / rebuild the full scene (this also creates Assets/Prefabs/*)
            SetupAll.SetupEverything();

            // 3) Apply sprites/glow to prefabs (Projectile/Enemies/XPOrb/HealthPack)
            // Must run AFTER SetupEverything so the prefab assets exist.
            SpriteSetup.ApplySpritesToPrefabs();

            // 4) Apply runtime sprite components to the newly spawned scene objects
            SpriteSetup.ApplySpritesToScene();

            // 5) Apply floor/background layering
            EnvironmentSprites.ApplyEnvironmentSprites();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FullSetupOneClick] ✓ FULL SETUP complete. Press Play.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FullSetupOneClick] FULL SETUP failed:\n" + ex);
        }
    }
}

