using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Decides when and where anomalies appear.
/// Unresolved anomalies stay put overnight, so ignoring them makes the house steadily creepier.
public class AnomalyDirector : MonoBehaviour
{
    [Header("Per-day spawns (from Day 2)")]
    public int firstAnomalyDay = 2;
    public int baseCount = 3;          // new anomalies on the first anomaly day
    public int extraPerDay = 2;        // added each day after that (3, 5, 7, 9...)
    public int maxNewPerDay = 9;
    public int maxActive = 16;         // never more than this many at once

    [Header("Mid-day surprises")]
    [Tooltip("From this day on, extra anomalies appear while the player is in another room.")]
    public int midDayFromDay = 3;
    [Tooltip("Surprises per day = day - 2, capped at this.")]
    public int maxMidDayPerDay = 4;
    public Vector2 midDayGap = new Vector2(15f, 35f);

    [Header("Picking")]
    [Tooltip("How much more likely a prop with hand-drawn anomaly art is to be picked.")]
    public float drawnArtWeight = 4f;

    [Header("Calls (anomalies give themselves away)")]
    [Tooltip("Every so often a random anomaly twitches and makes its sound. Quieter when it's in another room.")]
    public Vector2 callGap = new Vector2(7f, 14f);
    [Tooltip("Played for anomalies that have no sound of their own.")]
    public AudioClip callFallbackSound;

    [SerializeField] RoomCamera rooms;

    readonly List<Prop> props = new List<Prop>();
    Coroutine midDay, calls;

    void Awake()
    {
        props.AddRange(FindObjectsByType<Prop>(FindObjectsInactive.Include));
    }

    public int ActiveCount
    {
        get
        {
            int n = 0;
            foreach (var p in props) if (p.IsAnomalous && p.gameObject.activeInHierarchy) n++;
            return n;
        }
    }

    public void ResetAll()
    {
        foreach (var p in props) p.Resolve();
        StopMidDay();
    }

    public void OnDayStart(int day)
    {
        StopMidDay();
        if (day < firstAnomalyDay) return;

        int count = Mathf.Min(maxNewPerDay, baseCount + (day - firstAnomalyDay) * extraPerDay);
        count = Mathf.Min(count, maxActive - ActiveCount);
        for (int i = 0; i < count; i++) SpawnOne(false);

        if (day >= midDayFromDay) midDay = StartCoroutine(MidDay(Mathf.Min(maxMidDayPerDay, day - 2)));
        calls = StartCoroutine(Calls());
    }

    public void StopMidDay()
    {
        if (midDay != null) StopCoroutine(midDay);
        if (calls != null) StopCoroutine(calls);
        midDay = calls = null;
    }

    /// A random anomaly twitches and plays its sound now and then. Off-screen ones are only heard, faintly.
    IEnumerator Calls()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(callGap.x, callGap.y));
            if (GameManager.Instance != null && !GameManager.Instance.WorldInputEnabled) continue;
            var active = props.FindAll(p => p.IsAnomalous && p.gameObject.activeInHierarchy);
            if (active.Count == 0) continue;
            var p = active[Random.Range(0, active.Count)];
            bool visible = rooms == null || rooms.IsVisible(p.transform.position);
            if (visible) p.Wiggle(0.45f);
            Sfx.Play(p.clickSound != null ? p.clickSound : callFallbackSound, visible ? 0.45f : 0.18f, Random.Range(0.6f, 0.75f));
        }
    }

    IEnumerator MidDay(int surprises)
    {
        for (int i = 0; i < surprises; i++)
        {
            yield return new WaitForSeconds(Random.Range(midDayGap.x, midDayGap.y));
            if (ActiveCount < maxActive) SpawnOne(true);
        }
        midDay = null;
    }

    /// Turns one random eligible prop into an anomaly, favouring props with drawn anomaly art.
    /// If `offscreen`, only props the player can't currently see.
    public bool SpawnOne(bool offscreen)
    {
        var candidates = new List<Prop>();
        foreach (var p in props)
        {
            if (!p.CanBecomeAnomaly || p.IsAnomalous || !p.gameObject.activeInHierarchy) continue;
            if (offscreen && rooms != null && rooms.IsVisible(p.transform.position)) continue;
            candidates.Add(p);
        }
        while (candidates.Count > 0)
        {
            float total = 0f;
            foreach (var c in candidates) total += Weight(c);
            float roll = Random.value * total;
            int i = 0;
            for (; i < candidates.Count - 1; i++)
            {
                roll -= Weight(candidates[i]);
                if (roll <= 0f) break;
            }
            if (candidates[i].TriggerRandomAnomaly(Intensity)) return true;
            candidates.RemoveAt(i);
        }
        return false;
    }

    [Header("Escalation")]
    [Tooltip("Days from the first anomaly day until anomalies reach full strength (bold kinds unlock halfway).")]
    public int daysToFullIntensity = 4;

    /// 0 on the first anomaly day, 1 after daysToFullIntensity days.
    public float Intensity => Mathf.Clamp01((PlayerState.Day - firstAnomalyDay) / (float)Mathf.Max(1, daysToFullIntensity));

    float Weight(Prop p) => p.anomalySprite != null ? drawnArtWeight : 1f;
}
