using UnityEngine;

/// Put on every prop that must be bought before it appears in the room.
/// Do NOT put it on walls, floors or the background; those are always visible.
public class ItemSlot : MonoBehaviour
{
    public ItemDefinition item;
}
