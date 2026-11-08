using UnityEngine;

/// Marks a room object as buyable. It stays hidden until bought on the computer.
/// Put it on the same GameObject as the item's Prop.
public class ShopItem : MonoBehaviour
{
    public string id;
    public string displayName;
    [Tooltip("Hidden points. Must match the price printed on the shop card slot it appears in.")]
    public int price = 1;
    [Tooltip("Picture shown on the shop card. Defaults to the item's own sprite.")]
    public Sprite icon;

    public bool Owned => PlayerState.Owns(id);

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name.ToLowerInvariant().Replace(' ', '_');
        if (string.IsNullOrEmpty(displayName)) displayName = name;
    }

    public Sprite Icon
    {
        get
        {
            if (icon != null) return icon;
            var sr = GetComponent<SpriteRenderer>();
            return sr != null ? sr.sprite : null;
        }
    }

    /// Show it in the room with a pop.
    public void Reveal()
    {
        gameObject.SetActive(true);
        var prop = GetComponent<Prop>();
        if (prop != null) prop.Pop(0f);
    }
}
