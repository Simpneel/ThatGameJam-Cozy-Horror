using UnityEngine;

/// Finds the top-most Prop under the pointer each frame: hover feedback, then click.
/// Only Props with a Collider2D are clickable; walls and backgrounds are ignored.
public class ClickHandler : MonoBehaviour
{
    [SerializeField] Camera cam;

    Prop hovered;
    readonly System.Collections.Generic.List<Collider2D> hits = new System.Collections.Generic.List<Collider2D>();
    ContactFilter2D filter;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        filter = new ContactFilter2D { useTriggers = true };
        filter.NoFilter();
    }

    void Update()
    {
        Prop target = null;
        bool canInteract = GameManager.Instance == null || GameManager.Instance.WorldInputEnabled;
        if (canInteract && !GameInput.PointerOverUI && cam != null)
            target = PropUnderPointer();

        if (target != hovered)
        {
            if (hovered != null) hovered.Hovered = false;
            hovered = target;
            if (hovered != null) hovered.Hovered = true;
        }

        if (target != null && GameInput.ClickedThisFrame) target.Click();
    }

    Prop PropUnderPointer()
    {
        Vector2 world = cam.ScreenToWorldPoint(GameInput.PointerPosition);
        int count = Physics2D.OverlapPoint(world, filter, hits);
        Prop best = null;
        long bestKey = long.MinValue;
        for (int i = 0; i < count; i++)
        {
            var prop = hits[i].GetComponentInParent<Prop>();
            if (prop == null || !prop.isActiveAndEnabled) continue;
            var sr = prop.GetComponent<SpriteRenderer>();
            long key = ((long)SortingLayer.GetLayerValueFromID(sr.sortingLayerID) << 32) + sr.sortingOrder;
            if (best == null || key > bestKey) { best = prop; bestKey = key; }
        }
        return best;
    }
}
