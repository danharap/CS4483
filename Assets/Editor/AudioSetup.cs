using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets up audio clips for the game
/// </summary>
public static class AudioSetup
{
    [MenuItem("CS4483/🔊 Setup Game Audio")]
    public static void SetupAudio()
    {
        Debug.Log("[AudioSetup] Setting up game audio...");
        
        // Load audio clips
        AudioClip bulletSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aBullet.wav");
        AudioClip deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aDeath.wav");
        AudioClip flySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aFlyNoise.mp3");
        
        if (bulletSound == null)
        {
            Debug.LogError("[AudioSetup] Could not load aBullet.wav");
            return;
        }
        
        if (deathSound == null)
        {
            Debug.LogError("[AudioSetup] Could not load aDeath.wav");
            return;
        }
        
        // Apply to enemy prefabs
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Chaser.prefab", deathSound);
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Fast.prefab", deathSound);
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Boss.prefab", deathSound);
        
        // Apply to scene player (PlayerWeapon must be added via SetupAll)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerWeapon weapon = player.GetComponent<PlayerWeapon>();
            if (weapon != null)
            {
                SerializedObject so = new SerializedObject(weapon);
                so.FindProperty("shootSound").objectReferenceValue = bulletSound;
                so.ApplyModifiedProperties();
                Debug.Log("[AudioSetup] ✓ Applied bullet sound to player weapon");
            }
        }
        
        // Apply fly sound to all dead bodies in scene
        if (flySound != null)
        {
            DeadBody[] deadBodies = Object.FindObjectsOfType<DeadBody>();
            foreach (DeadBody db in deadBodies)
            {
                SerializedObject so = new SerializedObject(db);
                so.FindProperty("flySound").objectReferenceValue = flySound;
                so.ApplyModifiedProperties();
            }
            Debug.Log($"[AudioSetup] ✓ Applied fly sound to {deadBodies.Length} dead body/bodies");
        }
        else
        {
            Debug.LogWarning("[AudioSetup] aFlyNoise.mp3 not found; dead body fly sound not assigned.");
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("[AudioSetup] ✓ Game audio setup complete!");
    }
    
    private static void ApplyDeathSoundToEnemyPrefab(string prefabPath, AudioClip deathSound)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            EnemyBase enemy = root.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                SerializedObject so = new SerializedObject(enemy);
                so.FindProperty("deathSound").objectReferenceValue = deathSound;
                so.ApplyModifiedProperties();
                Debug.Log($"[AudioSetup] Applied death sound to {root.name}");
            }
        }
    }
}
