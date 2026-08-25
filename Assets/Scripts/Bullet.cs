using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Configuración del Proyectil")]
    public float speed = 50f;
    public int damage = 35;
    public float lifeTime = 2f;
    public string targetTag = "Enemy";

    [Header("Efectos Visuales")]
    public GameObject bloodVFXPrefab; 
    public GameObject wallVFXPrefab;  

    [Header("Game Feel: Impacto (¡NUEVO!)")]
    public float shakeDuration = 0.05f;   // Más corto que un golpe físico
    public float shakeMagnitude = 0.5f;   // Mucho más suave
    public float hitstopDuration = 0.02f; // Un micro-parón de tiempo
    [Range(0f, 1f)] public float hitstopTimeScale = 0.1f;

    void Start()
    {
        Destroy(gameObject, lifeTime); 
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Bullet")) return;

        if (other.CompareTag(targetTag))
        {
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            SpawnHitEffect(bloodVFXPrefab); 
            
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
            if (Hitstop.Instance != null) Hitstop.Instance.Freeze(hitstopDuration, hitstopTimeScale);

            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            SpawnHitEffect(wallVFXPrefab); 
            
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.02f, 0.2f);

            Destroy(gameObject); 
        }
    }
    
    private void SpawnHitEffect(GameObject vfxPrefab)
    {
        if (vfxPrefab != null)
        {
            Instantiate(vfxPrefab, transform.position, Quaternion.identity);
        }
    }
}