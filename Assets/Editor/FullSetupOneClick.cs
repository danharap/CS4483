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
            ZapTrapFrameSetup.SetupRedFrames();

            // 2) Build / rebuild the full scene (this also creates Assets/Prefabs/*)
            SetupAll.SetupEverything();

            // 3) Apply sprites/glow to prefabs (Projectile/Enemies/XPOrb/HealthPack)
            // Must run AFTER SetupEverything so the prefab assets exist.
            SpriteSetup.ApplySpritesToPrefabs();

            // 4) Apply runtime sprite components to the newly spawned scene objects
            SpriteSetup.ApplySpritesToScene();

            // 5) Apply floor/background layering
            EnvironmentSprites.ApplyEnvironmentSprites();

            // 6) Generate colosseum crowd on the stands
            GenerateCrowd();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[FullSetupOneClick] ✓ FULL SETUP complete. Press Play.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FullSetupOneClick] FULL SETUP failed:\n" + ex);
        }
    }

    private static void GenerateCrowd()
    {
        // Find or create a dedicated Crowd_Manager GameObject under the arena root.
        const string managerName = "Crowd_Manager";
        GameObject arenaRoot = GameObject.Find("=== LEVEL (ProBuilder) ===");
        Transform parent = arenaRoot != null ? arenaRoot.transform : null;

        // Reuse existing manager if already there, otherwise create one.
        ColosseumCrowdGenerator gen = Object.FindFirstObjectByType<ColosseumCrowdGenerator>();
        if (gen == null)
        {
            GameObject go = new GameObject(managerName);
            if (parent != null) go.transform.SetParent(parent, false);
            gen = go.AddComponent<ColosseumCrowdGenerator>();
        }

        gen.GenerateCrowd();
        Debug.Log("[FullSetupOneClick] ✓ Colosseum crowd generated.");
    }
}

