using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private Collider hitboxCollider;

    [Header("Combate")]
    public float damage = 25f;
    public string targetTag = "Enemy"; 

    [Header("Efectos Visuales y Cámara")]
    public GameObject hitVFXPrefab;
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 1.5f;

    [Header("Configuración de Hitstop")]
    public float hitstopDuration = 0.07f;
    [Range(0f, 1f)] public float hitstopTimeScale = 0.05f;

    void Start()
    {
        hitboxCollider = GetComponent<Collider>();
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            if (hitVFXPrefab != null)
            {
                Vector3 contactPoint = other.ClosestPoint(transform.position);
                Instantiate(hitVFXPrefab, contactPoint, Quaternion.identity);
            }

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
            }

            if (Hitstop.Instance != null)
            {
                Hitstop.Instance.Freeze(hitstopDuration, hitstopTimeScale);
            }

            if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }
        }
    }

    public void EnableHitbox()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null) hitboxCollider.enabled = false;
    }
}