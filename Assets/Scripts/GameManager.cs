using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Objetivo")]
    [Tooltip("Cantidad de estrellas necesarias para que aparezca el diamante")]
    public int starsRequired = 3;

    [Header("Diamante")]
    [Tooltip("El objeto del diamante en la escena que aparecerá al conseguir las estrellas")]
    public GameObject diamondObject;

    [Tooltip("Efecto de partículas opcional que aparecerá junto al diamante")]
    public GameObject diamondSpawnEffect;

    [Tooltip("Sonido opcional al aparecer el diamante")]
    public AudioClip diamondSpawnSound;

    private int starsCollected = 0;
    public int StarsCollected => starsCollected;

    private void Awake()
    {
        // Configurar Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ocultar el diamante al iniciar el juego
        if (diamondObject != null)
        {
            diamondObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("¡Atención! No has asignado el 'Diamond Object' en el GameManager.");
        }
    }

    public void AddStar()
    {
        starsCollected++;
        Debug.Log($"[GameManager] Estrella recogida ({starsCollected}/{starsRequired})");

        if (starsCollected == starsRequired)
        {
            SpawnDiamond();
        }
    }

    private void SpawnDiamond()
    {
        if (diamondObject == null) return;

        // Activar el diamante en la escena
        diamondObject.SetActive(true);

        // 1. Efecto de partículas / animación al aparecer
        if (diamondSpawnEffect != null)
        {
            Instantiate(diamondSpawnEffect, diamondObject.transform.position, Quaternion.identity);
        }

        // 2. Sonido al aparecer
        if (diamondSpawnSound != null)
        {
            AudioSource.PlayClipAtPoint(diamondSpawnSound, diamondObject.transform.position);
        }

        // 3. Animación de escala (crece suavemente desde 0 hasta su tamaño normal)
        StartCoroutine(PopInDiamond());
    }

    private IEnumerator PopInDiamond()
    {
        Vector3 targetScale = diamondObject.transform.localScale;
        diamondObject.transform.localScale = Vector3.zero;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Efecto suave tipo Ease-Out
            diamondObject.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            yield return null;
        }

        diamondObject.transform.localScale = targetScale;
    }
}
