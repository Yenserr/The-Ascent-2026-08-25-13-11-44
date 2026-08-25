using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    [Header("Eventos (Opcional)")]
    public UnityEvent onTakeDamage;
    public UnityEvent onDeath;

    private PlayerMovement playerMovement;

    private void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<PlayerMovement>();
    }

    /// <summary>
    /// Aplica daño al GameObject actual.
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (playerMovement != null && playerMovement.isInvulnerable)
        {
            Debug.Log($"[{gameObject.name}] ¡Ataque esquivado con I-Frames!");
            return;
        }

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        onTakeDamage?.Invoke();

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            if (playerMovement != null)
            {
                playerMovement.TriggerMercyIFrames();
            }
        }
    }

    private void Die()
    {
        onDeath?.Invoke();

        if (playerMovement == null)
        {
            Destroy(gameObject, 0.1f); 
        }
        else
        {
            Debug.Log("Elise ha sido derrotada.");
            playerMovement.HandleDeath(); 
        }
    }
}