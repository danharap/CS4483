using TMPro;
using UnityEngine;

/// <summary>
/// Floating damage number: moves upward and fades out, then destroys itself.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    public float floatSpeed = 2.2f;
    public float lifetime   = 0.7f;
    public Vector3 randomOffset = new Vector3(0.4f, 0.6f, 0.4f);

    private TextMeshPro text;
    private Color baseColor = Color.white;
    private float timer;

    public void Initialize(float amount, Color color)
    {
        if (text == null) text = GetComponent<TextMeshPro>();
        baseColor = color;

        if (text != null)
        {
            text.text  = Mathf.RoundToInt(amount).ToString();
            text.color = baseColor;
        }

        // Slight random horizontal offset so multiple numbers don't overlap perfectly
        transform.position += new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            Random.Range(0f, randomOffset.y),
            Random.Range(-randomOffset.z, randomOffset.z));
    }

    private void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        float t = Mathf.Clamp01(timer / lifetime);
        if (text != null)
        {
            Color c = baseColor;
            c.a = 1f - t;
            text.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}

