using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Decides when and where anomalies appear.
/// Unresolved anomalies stay put overnight, so ignoring them makes the house steadily creepier.
public class AnomalyDirector : MonoBehaviour
{
    [Header("Per-day spawns (from Day 2)")]
    public int firstAnomalyDay = 2;
    public int baseCount = 2;          // new anomalies on the first anomaly day
    public int extraPerDay = 1;        // added each day after that
    public int maxNewPerDay = 5;
    public int maxActive = 9;          // never more than this many at once

    [Header("Mid-day surprises")]
    [Tooltip("From this day on, an extra anomaly can appear while the player is in another room.")]
    public int midDayFromDay = 3;
    public Vector2 midDayDelay = new Vector2(25f, 55f);

    [SerializeField] RoomCamera rooms;

    readonly List<Prop> props = new List<Prop>();
    Coroutine midDay;

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

        if (day >= midDayFromDay) midDay = StartCoroutine(MidDay());
    }

    public void StopMidDay()
    {
        if (midDay != null) StopCoroutine(midDay);
        midDay = null;
    }

    IEnumerator MidDay()
    {
        yield return new WaitForSeconds(Random.Range(midDayDelay.x, midDayDelay.y));
        if (ActiveCount < maxActive) SpawnOne(true);
        midDay = null;
    }

    /// Turns one random eligible prop into an anomaly. If `offscreen`, only props the player can't currently see.
    public bool SpawnOne(bool offscreen)
    {
        var candidates = new List<Prop>();
        foreach (var p in props)
        {
            if (!p.canBeAnomalous || p.IsAnomalous || !p.gameObject.activeInHierarchy) continue;
            if (offscreen && rooms != null && rooms.IsVisible(p.transform.position)) continue;
            candidates.Add(p);
        }
        while (candidates.Count > 0)
        {
            int i = Random.Range(0, candidates.Count);
            if (candidates[i].TriggerRandomAnomaly()) return true;
            candidates.RemoveAt(i);
        }
        return false;
    }
}
