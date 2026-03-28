using UnityEngine;

/// <summary>
/// Generates a scattered placeholder crowd on the colosseum stands.
/// People are placed with random radial + angular offset so they look
/// naturally clustered rather than sitting in perfect rows.
///
/// Geometry matches ProBuilderLevelBuilder exactly:
///   arenaRadius = 38, wallHeight = 8, stepDepth = 1, stepHeight = 0.6
///   stepTopY(i) = 8.6 + i × 0.54   radialCentre(i) = 38.5 + i
///
/// First 2 steps are skipped (startRingOffset = 2) so nobody clips into the wall.
/// </summary>
[ExecuteAlways]
public class ColosseumCrowdGenerator : MonoBehaviour
{
    // ── Arena geometry (must match ProBuilderLevelBuilder) ────────────────

    [Header("Arena Geometry")]
    [SerializeField] private float arenaRadius    = 38f;
    [SerializeField] private float wallHeight     = 8f;
    [SerializeField] private float stepHalfHeight = 0.30f;   // 0.6 / 2
    [SerializeField] private float stepHeightInc  = 0.54f;   // 0.6 × 0.9
    [SerializeField] private float gapBehindWall  = 0.5f;
    [SerializeField] private float stepDepth      = 1.0f;

    // ── Ring selection ────────────────────────────────────────────────────

    [Header("Ring Selection")]
    [Tooltip("Skip this many inner rings. Set to 2 to avoid the rings nearest the wall.")]
    [SerializeField] private int   startRingOffset  = 2;
    [Tooltip("Total rings to fill after the offset.")]
    [SerializeField] private int   numberOfRings    = 10;

    // ── Density ───────────────────────────────────────────────────────────

    [Header("Density")]
    [Tooltip("Approximate people per ring before scatter. Each outer ring adds peopleRingStep more.")]
    [SerializeField] private int   basePeoplePerRing = 44;
    [SerializeField] private int   peopleRingStep    = 4;
    [Tooltip("Randomly leave seats empty for a natural look.")]
    [SerializeField] [Range(0f, 0.5f)] private float skipChance = 0.12f;

    // ── Scatter ───────────────────────────────────────────────────────────

    [Header("Scatter (key for natural look)")]
    [Tooltip("Random radial offset applied per person (±units outward/inward within the step).")]
    [SerializeField] private float radialScatter   = 0.35f;
    [Tooltip("Random angular offset applied per person (±degrees, breaks up the perfect circle).")]
    [SerializeField] private float angularScatter  = 6f;
    [Tooltip("Random Y offset per person so heights vary slightly across the step surface.")]
    [SerializeField] private float yScatter        = 0.10f;

    // ── Appearance ────────────────────────────────────────────────────────

    [Header("Capsule Appearance")]
    [SerializeField] private Vector3 capsuleScale         = new Vector3(0.55f, 0.50f, 0.55f);
    [SerializeField] [Range(0f, 0.25f)] private float randomScaleVariance   = 0.12f;
    [SerializeField] private float randomRotationVariance = 20f;
    [SerializeField] private Color crowdColor             = new Color(0.22f, 0.19f, 0.17f);

    [Header("Optional Prefab")]
    [Tooltip("If set, uses this prefab instead of a primitive capsule.")]
    [SerializeField] private GameObject personPrefab;

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = false;

    // ── Internal ──────────────────────────────────────────────────────────

    private const string RootName = "Crowd";

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    [ContextMenu("Generate Crowd")]
    public void GenerateCrowd()
    {
        ClearCrowd();

        GameObject crowdRoot = new GameObject(RootName);
        crowdRoot.transform.SetParent(transform, false);
        crowdRoot.transform.localPosition = Vector3.zero;

        Material mat = BuildMaterial();
        int total    = 0;

        for (int i = 0; i < numberOfRings; i++)
        {
            int   ringIdx = i + startRingOffset;         // skip first N rings near wall

            // Step geometry (exact match with ProBuilderLevelBuilder)
            float stepCentreY  = wallHeight + stepHalfHeight + ringIdx * stepHeightInc;
            float stepTopY     = stepCentreY + stepHalfHeight;   // top surface of this stair

            float baseRadius   = arenaRadius + gapBehindWall + ringIdx * stepDepth;
            float capsuleHalfH = capsuleScale.y;
            float baseY        = stepTopY + capsuleHalfH;        // capsule centre sits on stair top

            int count = basePeoplePerRing + i * peopleRingStep;
            total += PopulateRing(crowdRoot.transform, i, count, baseRadius, baseY, mat);
        }

        Debug.Log($"[ColosseumCrowd] Generated {total} people across {numberOfRings} rings " +
                  $"(rings {startRingOffset}–{startRingOffset + numberOfRings - 1}).");
    }

    [ContextMenu("Clear Crowd")]
    public void ClearCrowd()
    {
        Transform existing = transform.Find(RootName);
        if (existing == null) return;

        if (Application.isPlaying)
            Destroy(existing.gameObject);
        else
            DestroyImmediate(existing.gameObject);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Placement

    private int PopulateRing(Transform parent, int ringDisplayIdx, int count,
                             float baseRadius, float baseY, Material mat)
    {
        GameObject ringGO = new GameObject($"Ring_{ringDisplayIdx:D2}");
        ringGO.transform.SetParent(parent, false);

        float angleStep = 360f / count;
        int   placed    = 0;

        for (int i = 0; i < count; i++)
        {
            if (Random.value < skipChance) continue;

            // Base angle for this slot + random angular scatter
            float angleDeg = i * angleStep + Random.Range(-angularScatter, angularScatter);

            // Random scatter within the step's radial depth
            float radius = baseRadius + Random.Range(-radialScatter, radialScatter);

            // Small Y jitter so it doesn't look like a flat shelf
            float y = baseY + Random.Range(-yScatter, yScatter);

            SpawnPerson(ringGO.transform, radius, y, angleDeg, mat);
            placed++;
        }
        return placed;
    }

    private void SpawnPerson(Transform parent, float radius, float y, float angleDeg, Material mat)
    {
        float    rad   = angleDeg * Mathf.Deg2Rad;
        Vector3  pos   = new Vector3(Mathf.Sin(rad) * radius, y, Mathf.Cos(rad) * radius);

        GameObject person;

        if (personPrefab != null)
        {
#if UNITY_EDITOR
            person = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(personPrefab, parent);
#else
            person = Instantiate(personPrefab, Vector3.zero, Quaternion.identity, parent);
#endif
            person.transform.position = pos;
        }
        else
        {
            person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            person.transform.SetParent(parent, false);
            person.transform.position = pos;

            // Decorative only — kill the collider
            Collider col = person.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            Renderer rend = person.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
        }

        // Face the arena centre + random yaw jitter
        Vector3    inward  = new Vector3(-Mathf.Sin(rad), 0f, -Mathf.Cos(rad)); // toward 0,0,0
        Quaternion lookRot = Quaternion.LookRotation(inward, Vector3.up);
        float      yaw     = Random.Range(-randomRotationVariance, randomRotationVariance);
        person.transform.rotation = lookRot * Quaternion.Euler(0f, yaw, 0f);

        // Scale with per-person jitter
        float jitter = 1f + Random.Range(-randomScaleVariance, randomScaleVariance);
        person.transform.localScale = capsuleScale * jitter;

        person.name = $"P_{angleDeg:F0}";
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Helpers

    private Material BuildMaterial()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = crowdColor;
        mat.SetFloat("_Glossiness", 0.05f);
        mat.SetFloat("_Metallic",   0f);
        return mat;
    }

    private void Start()
    {
        if (generateOnStart && Application.isPlaying)
            GenerateCrowd();
    }

    #endregion
}
