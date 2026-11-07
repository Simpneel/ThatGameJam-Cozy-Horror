using UnityEngine;

/// Put on an item next to ItemSlot, SpriteRenderer and a Collider2D.
[RequireComponent(typeof(ItemSlot), typeof(SpriteRenderer))]
public class AnomalyItem : MonoBehaviour, IAnomalous
{
    SpriteRenderer sr;
    ItemSlot slot;
    Sprite originalSprite;
    Vector3 originalLocalPos;

    public bool IsActive { get; private set; }
    public AnomalyDefinition Current { get; private set; }
    public AnomalyDefinition[] Available => slot != null && slot.item != null ? slot.item.anomalies : null;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        slot = GetComponent<ItemSlot>();
        originalSprite = sr.sprite;
        originalLocalPos = transform.localPosition;
    }

    public void Trigger(AnomalyDefinition def)
    {
        if (IsActive || def == null) return;
        switch (def.effect)
        {
            case AnomalyEffect.SpriteSwap:
                if (def.swapSprite == null) return;
                sr.sprite = def.swapSprite;
                break;
            case AnomalyEffect.Hide:
                sr.enabled = false;
                break;
            case AnomalyEffect.Nudge:
                transform.localPosition = originalLocalPos + (Vector3)def.nudge;
                break;
        }
        Current = def;
        IsActive = true;
    }

    public void Resolve()
    {
        if (!IsActive) return;
        sr.sprite = originalSprite;
        sr.enabled = true;
        transform.localPosition = originalLocalPos;
        Current = null;
        IsActive = false;
    }
}
