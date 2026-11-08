using System;
using UnityEngine;

public enum AnomalyKind { None, SpriteSwap, Tilt, UpsideDown, Shift, Grow, Shrink, Tint }

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
    [Tooltip("Hand-drawn 'wrong' version. If empty, a procedural anomaly (tilt, shift, resize, tint) is used instead.")]
    public Sprite anomalySprite;
    [Tooltip("Scale applied while the anomaly sprite is shown (for anomaly art drawn at a different size).")]
    public float anomalySpriteScale = 1f;

    public AnomalyKind Anomaly { get; private set; }
    public bool IsAnomalous => Anomaly != AnomalyKind.None;

    /// Fired on a normal (non-anomaly) click. Bed and Computer listen to this.
    public event Action<Prop> Clicked;
    /// Fired when any anomaly is fixed.
    public static event Action<Prop> AnomalyFixed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { AnomalyFixed = null; }

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
            Sfx.Play(clickSound, volume, 0.75f);
            PlayerState.EarnPoint();
            AnomalyFixed?.Invoke(this);
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
    public bool TriggerRandomAnomaly()
    {
        Init();
        if (IsAnomalous || !canBeAnomalous) return false;
        if (anomalySprite != null && UnityEngine.Random.value < 0.75f) return Trigger(AnomalyKind.SpriteSwap);
        var options = new[] { AnomalyKind.Tilt, AnomalyKind.Tilt, AnomalyKind.Shift, AnomalyKind.Grow, AnomalyKind.Shrink, AnomalyKind.Tint, AnomalyKind.UpsideDown };
        return Trigger(options[UnityEngine.Random.Range(0, options.Length)]);
    }

    public bool Trigger(AnomalyKind kind)
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
                break;
            case AnomalyKind.Tilt: aRot = sign * UnityEngine.Random.Range(9f, 16f); break;
            case AnomalyKind.UpsideDown: aRot = 180f; break;
            case AnomalyKind.Shift: aPos = new Vector3(sign * Mathf.Clamp(size * 0.18f, 0.25f, 0.8f), 0f, 0f); break;
            case AnomalyKind.Grow: aScale = 1.22f; break;
            case AnomalyKind.Shrink: aScale = 0.78f; break;
            case AnomalyKind.Tint: sr.color = new Color(1f, 0.45f, 0.45f, normalColor.a); break;
        }
        Anomaly = kind;
        return true;
    }

    public void Resolve()
    {
        Init();
        sr.sprite = normalSprite;
        sr.color = normalColor;
        aPos = Vector3.zero;
        aRot = 0f;
        aScale = 1f;
        Anomaly = AnomalyKind.None;
    }

    void LateUpdate()
    {
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
