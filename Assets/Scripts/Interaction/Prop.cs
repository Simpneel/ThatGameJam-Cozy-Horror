using System;
using UnityEngine;

// Vanish is never picked at random: only the creep consequences use it (see Atmosphere).
public enum AnomalyKind { None, SpriteSwap, Tilt, Shift, Grow, Shrink, Tint, Vanish }

/// Anything in the room the player can click (Unreal: an Actor with an OnClicked event).
/// Handles hover feedback, the click wiggle + sound, and being an anomaly.
/// It owns its transform's pose: anomaly offsets and animations are layered on top of the pose it had at startup.
[RequireComponent(typeof(SpriteRenderer))]
public class Prop : MonoBehaviour
{
    [Header("Feel")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    [Header("Anomaly")]
    public bool canBeAnomalous = true;
    [Tooltip("Hand-drawn 'wrong' version. Used 90% of the time when set; otherwise one of allowedKinds.")]
    public Sprite anomalySprite;
    [Tooltip("Scale applied while the anomaly sprite is shown (for anomaly art drawn at a different size).")]
    public float anomalySpriteScale = 1f;
    [Tooltip("Position offset applied while the anomaly sprite is shown (drawn art that sits slightly differently).")]
    public Vector3 anomalyOffset;
    [Tooltip("Procedural anomalies that are clearly visible on this object. Filled in by the scene builder from the sprite's size, brightness and symmetry.")]
    public AnomalyKind[] allowedKinds = new AnomalyKind[0];

    /// Can the director turn this into an anomaly at all?
    public bool CanBecomeAnomaly => canBeAnomalous && (anomalySprite != null || (allowedKinds != null && allowedKinds.Length > 0));

    /// One shared "you fixed it" sound, set by the GameManager.
    public static AudioClip FixedSound;

    public AnomalyKind Anomaly { get; private set; }
    public bool IsAnomalous => Anomaly != AnomalyKind.None;

    /// Fired on a normal (non-anomaly) click. Bed and Computer listen to this.
    public event Action<Prop> Clicked;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { FixedSound = null; }

    SpriteRenderer sr;
    Sprite normalSprite;
    Color normalColor;
    Vector3 basePos, baseScale;
    float baseRot;

    // Anomaly pose offsets
    Vector3 aPos;
    float aRot;
    float aScale = 1f;

    // Animation state (unscaled time so it still works while paused UI is open)
    float hover, hoverTarget;
    float wiggleT = -1f, wiggleAmp;
    float popT = -1f, popFrom = 1f;
    bool initialised;

    const float HoverScale = 0.04f;
    const float WiggleDuration = 0.45f;
    const float PopDuration = 0.35f;

    void Awake() => Init();

    void Init()
    {
        if (initialised) return;
        initialised = true;
        sr = GetComponent<SpriteRenderer>();
        normalSprite = sr.sprite;
        normalColor = sr.color;
        basePos = transform.localPosition;
        baseRot = transform.localEulerAngles.z;
        baseScale = transform.localScale;
    }

    public bool Hovered { set => hoverTarget = value ? 1f : 0f; }

    public void Click()
    {
        if (IsAnomalous)
        {
            Resolve();
            Sfx.Play(FixedSound, 0.8f, UnityEngine.Random.Range(1.1f, 1.2f));
            Sfx.Play(clickSound, volume * 0.6f, 0.75f);
            PlayerState.EarnPoint();
            Pop(1.3f);
            return;
        }
        Wiggle();
        Sfx.Play(clickSound, volume, UnityEngine.Random.Range(0.94f, 1.06f));
        Clicked?.Invoke(this);
    }

    public void Wiggle(float strength = 1f) { wiggleT = 0f; wiggleAmp = 7f * strength; }
    public void Pop(float from = 0f) { popT = 0f; popFrom = from; }

    /// Picks a fitting anomaly for this prop. Returns false if it can't become one.
    /// intensity 0..1 grows over the days: early anomalies are mild, the red tint only unlocks at 0.5.
    public bool TriggerRandomAnomaly(float intensity = 0.5f)
    {
        Init();
        if (IsAnomalous || !CanBecomeAnomaly) return false;
        var kinds = new System.Collections.Generic.List<AnomalyKind>(allowedKinds ?? new AnomalyKind[0]);
        if (intensity < BoldFrom) kinds.RemoveAll(k => k == AnomalyKind.Tint);
        if (kinds.Count == 0 && allowedKinds != null) kinds.AddRange(allowedKinds); // nothing mild fits: allow anything
        if (anomalySprite != null && (kinds.Count == 0 || UnityEngine.Random.value < 0.9f)) return Trigger(AnomalyKind.SpriteSwap, intensity);
        return kinds.Count > 0 && Trigger(kinds[UnityEngine.Random.Range(0, kinds.Count)], intensity);
    }

    const float BoldFrom = 0.5f;

    public bool Trigger(AnomalyKind kind, float intensity = 0.5f)
    {
        Init();
        if (IsAnomalous || kind == AnomalyKind.None) return false;
        float size = sr.sprite != null ? Mathf.Max(sr.bounds.size.x, sr.bounds.size.y) : 1f;
        float sign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        switch (kind)
        {
            case AnomalyKind.SpriteSwap:
                if (anomalySprite == null) return false;
                sr.sprite = anomalySprite;
                aScale = anomalySpriteScale;
                aPos = anomalyOffset;
                break;
            case AnomalyKind.Tilt: aRot = sign * UnityEngine.Random.Range(7f, 10f) * Mathf.Lerp(1f, 2f, intensity); break; // 7-10° early, up to 14-20°
            case AnomalyKind.Shift: aPos = new Vector3(sign * Mathf.Clamp(size * Mathf.Lerp(0.13f, 0.25f, intensity), 0.25f, 1f), 0f, 0f); break;
            case AnomalyKind.Grow: aScale = Mathf.Lerp(1.15f, 1.3f, intensity); break;
            case AnomalyKind.Shrink: aScale = Mathf.Lerp(0.85f, 0.7f, intensity); break;
            case AnomalyKind.Tint: sr.color = new Color(1f, 0.45f, 0.45f, normalColor.a); break;
            case AnomalyKind.Vanish: SetVisible(false); break; // collider stays: click the empty spot to bring it back
        }
        Anomaly = kind;
        poseDirty = true;
        return true;
    }

    /// Shows/hides the sprite and anything attached (e.g. the headphone stand).
    void SetVisible(bool on)
    {
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = on;
    }

    public void Resolve()
    {
        Init();
        sr.sprite = normalSprite;
        sr.color = normalColor;
        if (Anomaly == AnomalyKind.Vanish) SetVisible(true);
        aPos = Vector3.zero;
        aRot = 0f;
        aScale = 1f;
        Anomaly = AnomalyKind.None;
        poseDirty = true;
    }

    // True when the anomaly pose changed and must be written once even if nothing is animating.
    bool poseDirty = true;

    void LateUpdate()
    {
        // Idle props don't touch their transform: every write would force a re-sync of the click collider.
        bool animating = hover != hoverTarget || wiggleT >= 0f || popT >= 0f;
        if (!animating && !poseDirty) return;
        poseDirty = false;

        float dt = Time.unscaledDeltaTime;
        hover = Mathf.MoveTowards(hover, hoverTarget, dt * 8f);
        float h = hover * hover * (3f - 2f * hover);

        float wig = 0f;
        if (wiggleT >= 0f)
        {
            wiggleT += dt;
            float k = 1f - wiggleT / WiggleDuration;
            if (k <= 0f) wiggleT = -1f;
            else wig = Mathf.Sin(wiggleT * 38f) * wiggleAmp * k * k;
        }

        float pop = 1f;
        if (popT >= 0f)
        {
            popT += dt;
            float t = Mathf.Clamp01(popT / PopDuration);
            if (popFrom < 1f)
            {
                float k = t - 1f; // ease-out-back from popFrom to 1
                pop = Mathf.LerpUnclamped(popFrom, 1f, 1f + 2.70158f * k * k * k + 1.70158f * k * k);
            }
            else pop = Mathf.Lerp(popFrom, 1f, 1f - (1f - t) * (1f - t)); // punch down to 1
            if (t >= 1f) popT = -1f;
        }

        transform.localPosition = basePos + aPos;
        transform.localRotation = Quaternion.Euler(0f, 0f, baseRot + aRot + wig);
        transform.localScale = baseScale * (aScale * (1f + HoverScale * h) * pop);
    }
}
