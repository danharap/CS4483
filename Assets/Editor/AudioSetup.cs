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
        
        // Load audio clips (Resources SFX preferred; legacy fallbacks)
        AudioClip bulletSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/SFX/WeaponEffect.mp3");
        if (bulletSound == null)
            bulletSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aBullet.wav");
        AudioClip deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/SFX/EnemyDeath.mp3");
        if (deathSound == null)
            deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aDeath.wav");
        AudioClip flySound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aFlyNoise.mp3");
        
        if (bulletSound == null)
        {
            Debug.LogError("[AudioSetup] Could not load weapon SFX (Resources/SFX/WeaponEffect.mp3 or aBullet.wav)");
            return;
        }
        
        if (deathSound == null)
        {
            Debug.LogError("[AudioSetup] Could not load enemy death SFX (Resources/SFX/EnemyDeath.mp3 or aDeath.wav)");
            return;
        }
        
        // Apply to enemy prefabs
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Chaser.prefab", deathSound);
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Fast.prefab", deathSound);
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Boss.prefab", deathSound);
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_Heavy.prefab", deathSound);
        ApplyDeathSoundToEnemyPrefab("Assets/Prefabs/Enemy_BigBat.prefab", deathSound);
        
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
        
        // Background music (Arena 1 / 2 / mini-boss / final boss)
        GameplayMusicController gmc = Object.FindFirstObjectByType<GameplayMusicController>();
        if (gmc != null)
        {
            AudioClip muLobby = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Music/LobbyMusic.mp3");
            AudioClip muA1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Music/Arena1.mp3");
            AudioClip muA2 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Music/Arena2.mp3");
            AudioClip muB1 = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Music/Boss1.mp3");
            AudioClip muFB = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Music/FinalBoss.mp3");
            SerializedObject soMu = new SerializedObject(gmc);
            soMu.FindProperty("lobbyMusic").objectReferenceValue     = muLobby;
            soMu.FindProperty("arena1Music").objectReferenceValue    = muA1;
            soMu.FindProperty("arena2Music").objectReferenceValue    = muA2;
            soMu.FindProperty("boss1Music").objectReferenceValue     = muB1;
            soMu.FindProperty("finalBossMusic").objectReferenceValue = muFB;
            soMu.ApplyModifiedProperties();
            Debug.Log("[AudioSetup] ✓ Wired gameplay music clips");
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
