using System.Collections;
using UnityEngine;

/// Tiny tween helpers. All use unscaled time so they keep working while the game is paused.
public static class UIAnim
{
    public static IEnumerator Fade(CanvasGroup g, float to, float duration)
    {
        if (g == null) yield break;
        float from = g.alpha, t = 0f;
        while (t < duration)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f); // a hitch frame must not skip the animation
            g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        g.alpha = to;
    }

    /// Scales from `from` to 1 with a slight overshoot.
    public static IEnumerator PopIn(Transform t, float from = 0.85f, float duration = 0.25f)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float k = Mathf.Clamp01(time / duration) - 1f;
            float s = 1f + 2.70158f * k * k * k + 1.70158f * k * k;
            t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, s);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    public static IEnumerator Shake(RectTransform t, float strength = 10f, float duration = 0.3f)
    {
        Vector2 origin = t.anchoredPosition;
        float time = 0f;
        while (time < duration)
        {
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float k = 1f - time / duration;
            t.anchoredPosition = origin + new Vector2(Mathf.Sin(time * 70f) * strength * k, 0f);
            yield return null;
        }
        t.anchoredPosition = origin;
    }

    public static IEnumerator SlideY(RectTransform t, float fromY, float toY, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float k = Mathf.Clamp01(time / duration);
            k = 1f - (1f - k) * (1f - k) * (1f - k);
            t.anchoredPosition = new Vector2(t.anchoredPosition.x, Mathf.Lerp(fromY, toY, k));
            yield return null;
        }
        t.anchoredPosition = new Vector2(t.anchoredPosition.x, toY);
    }
}
