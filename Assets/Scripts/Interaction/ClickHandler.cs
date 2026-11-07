using System;
using UnityEngine;

/// Turns clicks into finds or misses. Only objects with a Collider2D can be clicked.
public class ClickHandler : MonoBehaviour
{
    public static event Action<AnomalyDefinition> OnFound;
    public static event Action OnMiss;

    /// Set true while the PC overlay (or any UI) is open so clicks don't reach the room.
    public static bool InputBlocked;

    [SerializeField] Camera cam;
    [SerializeField] float missPenalty = 0.5f;   // hidden score lost on a wrong click
    [SerializeField] AudioClip missSound;

    AudioSource audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { OnFound = null; OnMiss = null; InputBlocked = false; }

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (InputBlocked || !GameInput.Click(out Vector2 screenPos)) return;

        Vector2 world = cam.ScreenToWorldPoint(screenPos);
        Collider2D best = null;
        int bestOrder = int.MinValue;
        foreach (var hit in Physics2D.OverlapPointAll(world))
        {
            var sr = hit.GetComponent<SpriteRenderer>();
            int order = sr != null ? sr.sortingOrder : 0;
            if (best == null || order > bestOrder) { best = hit; bestOrder = order; }
        }
        if (best == null) return; // clicked empty space: ignored

        var anomalous = best.GetComponentInParent<IAnomalous>();
        if (anomalous != null && anomalous.IsActive)
        {
            var def = anomalous.Current;
            anomalous.Resolve();
            PlayerState.AddMoney(def.reward);
            PlayerState.AddScore(def.scoreGain);
            if (def.foundSound != null) audioSource.PlayOneShot(def.foundSound);
            OnFound?.Invoke(def);
        }
        else
        {
            PlayerState.AddScore(-missPenalty);
            if (missSound != null) audioSource.PlayOneShot(missSound);
            OnMiss?.Invoke();
        }
    }
}
