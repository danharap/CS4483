using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public float healAmount = 20f;
    public float lifetime = 10f;
    
    private float spawnTime;
    
    void Start()
    {
        spawnTime = Time.time;
    }
    
    void Update()
    {
        // Despawn after lifetime
        if (Time.time - spawnTime > lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null && ph.Health < ph.MaxHealth)
            {
                ph.Health = Mathf.Min(ph.Health + healAmount, ph.MaxHealth);
                Debug.Log($"[HealthPack] Player healed {healAmount} HP!");
                Destroy(gameObject);
            }
        }
    }
}
