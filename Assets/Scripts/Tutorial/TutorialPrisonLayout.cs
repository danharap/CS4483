using UnityEngine;

/// <summary>
/// Single source of truth for rebuilt tutorial prison bounds and spawn (matches ProBuilderLevelBuilder.TutorialPrisonConstants).
/// </summary>
public static class TutorialPrisonLayout
{
    public const float CorridorCenterZ = -40f;
    public const float FloorHalfX = 85f;
    public const float FloorHalfZ = 7f;
    /// <summary>Player start: inside walkway, west of first gate, on corridor centerline Z.</summary>
    public static readonly Vector3 PlayerSpawnPosition = new Vector3(-74f, 1.1f, -40f);
}
