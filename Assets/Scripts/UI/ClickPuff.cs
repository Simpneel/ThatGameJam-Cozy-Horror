using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// A small "..." that floats up and fades where you clicked on nothing, so a miss doesn't feel dead.
public class ClickPuff : MonoBehaviour
{
    [SerializeField] RectTransform canvasRect;
    [SerializeField] RectTransform puff;
    [SerializeField] CanvasGroup group;

    Coroutine running;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public void Show(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out var local);
        puff.anchoredPosition = local + new Vector2(0f, 20f);
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        Vector2 start = puff.anchoredPosition;
        float t = 0f;
        const float duration = 0.6f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float k = t / duration;
            puff.anchoredPosition = start + new Vector2(0f, 30f * k);
            group.alpha = k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f;
            yield return null;
        }
        group.alpha = 0f;
        running = null;
    }
}
