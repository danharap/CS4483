using TMPro;
using UnityEngine;

/// <summary>
/// Lobby Merchant NPC. When the player walks within interact range and presses E,
/// the Skill Tree UI opens. Shows a "Press E" prompt when in range.
/// Displays a 2D sprite billboard with an occasional blink animation.
/// </summary>
public class LobbyMerchant : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private KeyCode interactKey  = KeyCode.E;

    [Header("References")]
    [SerializeField] private SkillTreeUI skillTreeUI;
    [SerializeField] private TMP_Text    promptText;

    [Header("Sprite")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite blinkSprite;
    [SerializeField] private float blinkDuration = 0.12f;
    [SerializeField] private float blinkIntervalMin = 2f;
    [SerializeField] private float blinkIntervalMax = 5f;

    private Transform playerTransform;
    private bool playerInRange = false;

    private SpriteRenderer spriteRenderer;
    private float nextBlinkTime;
    private float blinkEndTime;
    private bool isBlinking;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (skillTreeUI == null)
            skillTreeUI = Object.FindFirstObjectByType<SkillTreeUI>(FindObjectsInactive.Include);

        if (skillTreeUI == null)
            Debug.LogWarning("[LobbyMerchant] SkillTreeUI not found — re-run CS4483 → SETUP EVERYTHING.");

        if (promptText) promptText.gameObject.SetActive(false);

        SetupSprite();
        ScheduleNextBlink();
    }

    private void SetupSprite()
    {
        // Hide the 3D mesh (capsule) if one exists from older setup
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Reuse existing sprite child or create a new one
        Transform existing = transform.Find("Merchant_Sprite");
        GameObject spriteObj;
        if (existing != null)
        {
            spriteObj = existing.gameObject;
            spriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            spriteObj = new GameObject("Merchant_Sprite");
            spriteObj.transform.SetParent(transform, false);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sortingOrder = 10;

        if (idleSprite != null)
        {
            spriteRenderer.sprite = idleSprite;
        }
        else
        {
            Debug.LogWarning("[LobbyMerchant] idleSprite is null — run CS4483 → Slice Sprite Sheets, then SETUP EVERYTHING.");
        }

        if (spriteObj.GetComponent<Billboard>() == null)
            spriteObj.AddComponent<Billboard>();
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
    }

    void Update()
    {
        UpdateBlink();
        UpdateInteraction();
    }

    private void UpdateBlink()
    {
        if (spriteRenderer == null || idleSprite == null || blinkSprite == null) return;

        if (isBlinking)
        {
            if (Time.time >= blinkEndTime)
            {
                spriteRenderer.sprite = idleSprite;
                isBlinking = false;
                ScheduleNextBlink();
            }
        }
        else if (Time.time >= nextBlinkTime)
        {
            spriteRenderer.sprite = blinkSprite;
            blinkEndTime = Time.time + blinkDuration;
            isBlinking = true;
        }
    }

    private void UpdateInteraction()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (promptText) promptText.gameObject.SetActive(inRange);
        }

        if (inRange && Input.GetKeyDown(interactKey))
        {
            skillTreeUI?.Show();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
