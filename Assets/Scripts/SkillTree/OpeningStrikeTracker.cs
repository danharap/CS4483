using UnityEngine;

/// <summary>
/// Capstone: "Opening Strike" (meta_first_blood)
/// The player's first projectile fired each wave deals 100% bonus damage (2× multiplier).
/// Gets added to the player when the skill is unlocked and is removed when the run ends /
/// player respawns (GameManager calls ResetToBase which destroys and re-creates passives).
/// </summary>
public class OpeningStrikeTracker : MonoBehaviour
{
    private bool readyToProc = false;
    private WaveManager waveManager;

    void Start()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnWaveStart += OnWaveStart;
            // Arm for the first wave immediately
            readyToProc = true;
        }
    }

    void OnDestroy()
    {
        if (waveManager != null)
            waveManager.OnWaveStart -= OnWaveStart;
    }

    private void OnWaveStart(int waveIndex)
    {
        readyToProc = true;
    }

    /// <summary>Called by PlayerWeapon right before setting projectile damage. Returns the bonus multiplier.</summary>
    public float ConsumeProcMultiplier()
    {
        if (!readyToProc) return 1f;
        readyToProc = false;
        return 2f; // double damage
    }
}
