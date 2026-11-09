using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Short message in plain white text on a dark grey box, e.g. "CAN'T AFFORD".
public class Toast : MonoBehaviour
{
    [SerializeField] Text label;
    [SerializeField] CanvasGroup group;
    [SerializeField] float holdTime = 1.1f;

    Coroutine running;
    float minWidth, currentHold;

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public void Show(string message) => Show(message, holdTime);

    /// Widens the box to fit the text (never narrower than built) so long messages stay on one line.
    public void Show(string message, float hold)
    {
        label.text = message;
        var rt = (RectTransform)transform;
        if (minWidth <= 0f) minWidth = rt.sizeDelta.x;
        rt.sizeDelta = new Vector2(Mathf.Max(minWidth, label.preferredWidth + 70f), rt.sizeDelta.y);
        currentHold = hold;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        group.alpha = 1f;
        StartCoroutine(UIAnim.Shake((RectTransform)transform, 8f, 0.25f));
        float t = 0f;
        while (t < currentHold) { t += Time.unscaledDeltaTime; yield return null; }
        yield return UIAnim.Fade(group, 0f, 0.3f);
        running = null;
    }
}
