using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// Mood that reacts to the game:
/// - Creep: the more anomalies are left unfixed, the darker the screen edges and the more the music detunes.
/// - Day pacing: morning, afternoon, evening, night, each a visible lighting shift; from evening a warm halo pulses behind the bed.
public class Atmosphere : MonoBehaviour
{
    [Header("Creep (unfixed anomalies)")]
    [Tooltip("Number of unfixed anomalies at which the creep effect is at full strength.")]
    public int creepFullAt = 8;
    [Range(0f, 1f)] public float maxVignette = 0.85f;
    [Range(0f, 0.3f)] public float maxDetune = 0.12f;

    [Header("Day pacing")]
    [Tooltip("Seconds from morning to night. Time keeps running in the shop and other menus; it stops only for pause and room transitions.")]
    public float dayLength = 120f;
    [Tooltip("Times of day, evenly spread over the day: Morning, Afternoon, Evening, Night. Each change eases in over phaseBlend seconds.")]
    public float[] phaseBrightness = { 1f, 0.88f, 0.68f, 0.4f };
    public Color[] phaseColor = { Color.white, new Color(1f, 0.93f, 0.8f), new Color(1f, 0.74f, 0.58f), new Color(0.55f, 0.62f, 1f) };
    public float phaseBlend = 3f;
    [Tooltip("Fraction of the day after which the bed starts to glow.")]
    [Range(0f, 1f)] public float bedGlowFrom = 0.5f;

    [Header("Refs")]
    public AnomalyDirector director;
    public MusicPlayer music;
    public Image vignette;
    public Light2D globalLight;
    [Tooltip("Warm halo behind the bed (unlit, so it shows through the dusk).")]
    public SpriteRenderer bedGlow;
    [Range(0f, 1f)] public float bedGlowAlpha = 0.6f;
    public Prop bed;

    public float DayProgress { get; private set; }
    public float Creep { get; private set; }

    bool running;
    float nextBedNudge, nextCreepPoll, creepTarget;

    /// Only touches the UI when the value really changed (each change re-batches its canvas), and hides it at zero.
    static void SetAlpha(Graphic g, float a)
    {
        bool visible = a > 0.005f;
        if (g.enabled != visible) g.enabled = visible;
        if (!visible || Mathf.Abs(g.color.a - a) < 0.004f) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }

    /// 0 Morning, 1 Afternoon, 2 Evening, 3 Night.
    public int Phase { get; private set; }

    public void StartDay()
    {
        DayProgress = 0f;
        Phase = 0;
        if (globalLight != null) { globalLight.intensity = phaseBrightness[0]; globalLight.color = phaseColor[0]; } // snap to morning (screen is black)
        running = true;
        nextBedNudge = 0f;
    }

    public void Stop() => running = false;

    void Update()
    {
        // Creep follows the number of unfixed anomalies, eased so it swells and fades rather than jumping.
        // The anomaly count only changes on spawns/fixes, so polling it 4x a second is plenty.
        if (Time.unscaledTime >= nextCreepPoll && director != null)
        {
            creepTarget = Mathf.Clamp01(director.ActiveCount / (float)Mathf.Max(1, creepFullAt));
            nextCreepPoll = Time.unscaledTime + 0.25f;
        }
        Creep = Mathf.MoveTowards(Creep, creepTarget, Time.unscaledDeltaTime * 0.5f);
        if (vignette != null)
        {
            float breathe = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 1.7f) * Creep;
            SetAlpha(vignette, maxVignette * Creep * breathe);
        }
        if (music != null) music.detune = maxDetune * Creep;

        // Day clock only runs while the player is free to play.
        bool clock = GameManager.Instance != null && GameManager.Instance.ClockRunning;
        bool playing = GameManager.Instance != null && GameManager.Instance.WorldInputEnabled;
        if (running && clock) DayProgress = Mathf.Min(1f, DayProgress + Time.deltaTime / Mathf.Max(1f, dayLength));

        // Stepped times of day: a noticeable shift every quarter of the day rather than an invisible slow fade.
        Phase = Mathf.Min(phaseBrightness.Length - 1, Mathf.FloorToInt(DayProgress * phaseBrightness.Length));
        if (globalLight != null)
        {
            float step = Time.deltaTime / Mathf.Max(0.01f, phaseBlend);
            globalLight.intensity = Mathf.MoveTowards(globalLight.intensity, phaseBrightness[Phase], step);
            globalLight.color = Color.Lerp(globalLight.color, phaseColor[Phase], step * 2f);
        }

        float glow = Mathf.Clamp01((DayProgress - bedGlowFrom) / Mathf.Max(0.01f, 1f - bedGlowFrom));
        if (bedGlow != null)
        {
            float a = glow * bedGlowAlpha * (0.75f + 0.25f * Mathf.Sin(Time.time * 2.2f));
            bedGlow.enabled = a > 0.005f; // don't draw a big transparent sprite all morning
            if (bedGlow.enabled) { var c = bedGlow.color; c.a = a; bedGlow.color = c; }
        }

        // Once it's fully evening, the bed gives a little nudge every few seconds.
        if (running && playing && DayProgress >= 1f && bed != null && Time.time >= nextBedNudge)
        {
            bed.Wiggle(0.35f);
            nextBedNudge = Time.time + 5f;
        }
    }
}
