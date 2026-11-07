using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Hides unowned items on load and reveals them (with a small pop-in) when bought.
public class RoomPopulator : MonoBehaviour
{
    readonly List<ItemSlot> slots = new List<ItemSlot>();

    void Awake()
    {
        slots.AddRange(FindObjectsByType<ItemSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    void OnEnable()  { PlayerState.OnItemOwned += HandleOwned; }
    void OnDisable() { PlayerState.OnItemOwned -= HandleOwned; }

    void Start()
    {
        foreach (var slot in slots)
        {
            if (slot.item == null) continue;
            slot.gameObject.SetActive(PlayerState.Owns(slot.item.id));
        }
    }

    void HandleOwned(string id)
    {
        foreach (var slot in slots)
        {
            if (slot.item == null || slot.item.id != id || slot.gameObject.activeSelf) continue;
            slot.gameObject.SetActive(true);
            StartCoroutine(PopIn(slot.transform));
        }
    }

    IEnumerator PopIn(Transform t)
    {
        const float duration = 0.25f;
        Vector3 target = t.localScale;
        t.localScale = Vector3.zero;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / duration) - 1f;
            float s = 1f + 2.70158f * k * k * k + 1.70158f * k * k; // ease-out-back
            t.localScale = target * s;
            yield return null;
        }
        t.localScale = target;
    }
}
