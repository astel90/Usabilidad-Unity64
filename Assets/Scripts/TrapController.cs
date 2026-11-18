using System.Collections;
using UnityEngine;

public class TrapController : MonoBehaviour
{
    [Header("Configuración de Cooldown")]
    [Tooltip("Tiempo en segundos antes de que la trampa pueda volver a activarse")]
    public float cooldown = 3.0f;

    [Header("Animación")]
    [Tooltip("Animator de la trampa. Si se deja vacío, lo buscará automáticamente en este objeto o en sus hijos")]
    public Animator trapAnimator;

    [Tooltip("Nombre del parámetro Trigger en el Animator para activar la trampa (por defecto: 'Snap')")]
    public string triggerParameter = "Snap";

    [Tooltip("Si prefieres reproducir directamente un estado por nombre (opcional, ej: 'Take 001' o 'Anims')")]
    public string directStateName = "";

    [Header("Efecto de Partículas")]
    [Tooltip("Prefab del efecto de partículas a instanciar (por ejemplo de Cartoon FX)")]
    public GameObject particleEffectPrefab;

    [Tooltip("Punto donde aparecerán las partículas (si está vacío, se usarán las coordenadas de la trampa)")]
    public Transform effectSpawnPoint;

    [Header("Sonidos (Opcionales)")]
    [Tooltip("Sonido de la trampa al cerrarse / activarse")]
    public AudioClip trapSound;

    [Tooltip("Sonido opcional cuando la trampa vuelve a abrirse")]
    public AudioClip rearmSound;

    public static event System.Action Activada;

    private bool canTrigger = true;

    private void Awake()
    {
        if (trapAnimator == null)
        {
            trapAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Reset()
    {
        if (trapAnimator == null)
        {
            trapAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void OnValidate()
    {
        if (trapAnimator == null)
        {
            trapAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckAndTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Por si el jugador se queda parado encima de la trampa
        CheckAndTrigger(other);
    }

    private void CheckAndTrigger(Collider other)
    {
        if (!canTrigger) return;

        // Verificar si quien entró es el jugador
        if (other.CompareTag("Player") ||
            other.GetComponent<CharacterController>() != null ||
            other.GetComponentInParent<CharacterController>() != null)
        {
            StartCoroutine(ActivateTrap());
        }
    }

    private IEnumerator ActivateTrap()
    {
        canTrigger = false;

        // 1. Disparar animación de cierre
        if (trapAnimator != null)
        {
            if (!string.IsNullOrEmpty(directStateName))
            {
                trapAnimator.Play(directStateName, 0, 0f);
            }
            else if (!string.IsNullOrEmpty(triggerParameter))
            {
                trapAnimator.SetTrigger(triggerParameter);
            }
        }

        // 2. Generar efecto de partículas
        Vector3 spawnPos = effectSpawnPoint != null ? effectSpawnPoint.position : transform.position;
        if (particleEffectPrefab != null)
        {
            Instantiate(particleEffectPrefab, spawnPos, Quaternion.identity);
        }

        // 3. Reproducir sonido al cerrarse
        if (trapSound != null)
        {
            AudioSource.PlayClipAtPoint(trapSound, spawnPos);
        }

        Activada?.Invoke();

        // 4. Esperar el tiempo de cooldown (3 segundos) con la trampa cerrada
        yield return new WaitForSeconds(cooldown);

        // 5. Reabrir automáticamente la trampa y dejarla lista
        if (trapAnimator != null)
        {
            // Rebind reinicia el modelo a su postura original (abierta) y resetea el Animator
            trapAnimator.Rebind();
            trapAnimator.Update(0f);
        }

        // Sonido opcional al rearmarse
        if (rearmSound != null)
        {
            AudioSource.PlayClipAtPoint(rearmSound, spawnPos);
        }

        canTrigger = true;
    }
}
