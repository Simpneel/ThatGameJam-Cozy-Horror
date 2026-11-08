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
        filter = ContactFilter2D.noFilter;
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

        // Only accept the click if the room was already clickable at the end of last frame.
        // Otherwise the click that closes a menu (shop ✕, flyer X, "No", "Resume") would fall through onto the room.
        if (target != null && interactableLastFrame && GameInput.ClickedThisFrame) target.Click();
    }

    bool interactableLastFrame;

    // LateUpdate runs after the UI has handled this frame's clicks, so this reflects the state the next frame starts in.
    void LateUpdate() => interactableLastFrame = GameManager.Instance == null || GameManager.Instance.WorldInputEnabled;

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
