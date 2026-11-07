using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Temporary test harness. Remove once the shop and director exist.
/// 1 = grant next item   2 = trigger a random anomaly   3 = reset save and reload
public class DebugTools : MonoBehaviour
{
    [SerializeField] ItemDefinition[] allItems;

    void Update()
    {
        if (GameInput.DebugKey(1)) GrantNextItem();
        if (GameInput.DebugKey(2)) TriggerRandomAnomaly();
        if (GameInput.DebugKey(3))
        {
            PlayerState.ResetAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void GrantNextItem()
    {
        foreach (var item in allItems)
        {
            if (item != null && PlayerState.Own(item.id)) return;
        }
    }

    void TriggerRandomAnomaly()
    {
        var candidates = new List<AnomalyItem>();
        foreach (var a in FindObjectsByType<AnomalyItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (!a.IsActive && a.Available != null && a.Available.Length > 0) candidates.Add(a);

        if (candidates.Count == 0) { Debug.Log("No item available for an anomaly."); return; }
        var target = candidates[Random.Range(0, candidates.Count)];
        target.Trigger(target.Available[Random.Range(0, target.Available.Length)]);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 600, 30),
            $"Money {PlayerState.Money}   Score {PlayerState.Score:0.0}   [1] grant item  [2] trigger anomaly  [3] reset");
    }
}
