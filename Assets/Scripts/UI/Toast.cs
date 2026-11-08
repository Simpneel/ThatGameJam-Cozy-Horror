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

    void Awake()
    {
        group.alpha = 0f;
        group.blocksRaycasts = false;
    }

    public void Show(string message)
    {
        label.text = message;
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        group.alpha = 1f;
        StartCoroutine(UIAnim.Shake((RectTransform)transform, 8f, 0.25f));
        float t = 0f;
        while (t < holdTime) { t += Time.unscaledDeltaTime; yield return null; }
        yield return UIAnim.Fade(group, 0f, 0.3f);
        running = null;
    }
}
