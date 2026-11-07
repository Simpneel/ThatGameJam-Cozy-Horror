using UnityEngine;

[CreateAssetMenu(menuName = "Anomaly Game/Item")]
public class ItemDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public int price = 50;
    public Sprite shopIcon;
    public ItemDefinition[] requires;       // must own these first
    public float minScore;                  // hidden score needed before it is offered
    public AnomalyDefinition[] anomalies;   // what can go wrong with this item

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name.ToLowerInvariant().Replace(' ', '_');
        if (string.IsNullOrEmpty(displayName)) displayName = name;
    }

    /// Can the shop/ads offer this right now?
    public bool IsOffered()
    {
        if (PlayerState.Owns(id)) return false;
        if (PlayerState.Score < minScore) return false;
        if (requires != null)
            foreach (var r in requires)
                if (r != null && !PlayerState.Owns(r.id)) return false;
        return true;
    }
}
