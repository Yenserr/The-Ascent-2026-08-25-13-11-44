using System.Collections;
using UnityEngine;

public class Hitstop : MonoBehaviour
{
    public static Hitstop Instance { get; private set; }

    private bool isWaiting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Congela el tiempo brevemente al impactar.
    /// </summary>
    /// <param name="duration">Duración en segundos reales (ej: 0.07 para 70ms)</param>
    /// <param name="timeScale">Escala de tiempo durante el golpe (ej: 0.05f)</param>
    public void Freeze(float duration = 0.07f, float timeScale = 0.05f)
    {
        if (isWaiting) return;
        StartCoroutine(DoHitstop(duration, timeScale));
    }

    private IEnumerator DoHitstop(float duration, float timeScale)
    {
        isWaiting = true;
        Time.timeScale = timeScale;

        // Importante: usar Realtime para que continúe aunque el timeScale sea casi 0
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1f;
        isWaiting = false;
    }
}