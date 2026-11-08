using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Bottom-left room name, shown for a few seconds whenever the camera arrives in a room.
public class RoomLabel : MonoBehaviour
{
    [SerializeField] RoomCamera rooms;
    [SerializeField] Text label;
    [SerializeField] CanvasGroup group;
    public float visibleSeconds = 3f;

    Coroutine running;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
        rooms.RoomChanged += Show;
    }

    void OnDestroy()
    {
        if (rooms != null) rooms.RoomChanged -= Show;
    }

    void Show(int index)
    {
        label.text = rooms.rooms[index].name.ToUpperInvariant();
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        yield return UIAnim.Fade(group, 1f, 0.2f);
        float t = 0f;
        while (t < visibleSeconds) { t += Time.unscaledDeltaTime; yield return null; }
        yield return UIAnim.Fade(group, 0f, 0.5f);
        running = null;
    }
}
