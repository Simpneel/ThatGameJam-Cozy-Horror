using UnityEngine;

public enum AnomalyEffect
{
    SpriteSwap, // show swapSprite instead of the normal sprite (your drawn pairs)
    Hide,       // the item disappears (its collider stays clickable)
    Nudge       // the item shifts by `nudge` world units
}

[CreateAssetMenu(menuName = "Anomaly Game/Anomaly")]
public class AnomalyDefinition : ScriptableObject
{
    public string id;
    public AnomalyEffect effect;
    public Sprite swapSprite;                    // for SpriteSwap
    public Vector2 nudge = new Vector2(0.3f, 0); // for Nudge
    [Range(0f, 1f)] public float subtlety = 0.5f; // used later by the director
    public int reward = 10;                      // money on a correct click
    public float scoreGain = 1f;                 // hidden score on a correct click
    public AudioClip foundSound;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name.ToLowerInvariant().Replace(' ', '_');
    }
}
