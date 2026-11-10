using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// Full-screen layers: title, win/lose, credits, the black fade, the day banner and the transition video.
public class Screens : MonoBehaviour
{
    [Header("Full-screen pages (each has a Button covering it)")]
    public Button titleScreen;
    public Button winScreen;
    public Button loseScreen;
    public Button creditsScreen;

    [Header("Overlays")]
    public CanvasGroup fade;
    public CanvasGroup dayBanner;
    public Text dayBannerLabel;

    [Header("Transition video")]
    public RawImage videoImage;
    public VideoPlayer video;
    [Tooltip("File inside Assets/StreamingAssets. Streaming is the only way video works in WebGL builds.")]
    public string videoFile = "VID_Transition.mp4";

    RenderTexture videoTexture;

    void Awake()
    {
        foreach (var b in new[] { titleScreen, winScreen, loseScreen, creditsScreen }) b.gameObject.SetActive(false);
        videoImage.gameObject.SetActive(false);
        dayBanner.alpha = 0f;
        fade.alpha = 1f;
        fade.blocksRaycasts = true;
    }

    // ---------- pages ----------

    /// fadeIn = false shows the page at once (used when replacing another full-screen page, so nothing shows through).
    public void ShowPage(Button page, Action onClick, bool fadeIn = true)
    {
        page.onClick.RemoveAllListeners();
        page.onClick.AddListener(() => { page.onClick.RemoveAllListeners(); onClick?.Invoke(); });
        page.gameObject.SetActive(true);
        var g = page.GetComponent<CanvasGroup>();
        if (g == null) return;
        if (fadeIn) { g.alpha = 0f; StartCoroutine(UIAnim.Fade(g, 1f, 0.6f)); }
        else g.alpha = 1f;
    }

    public void HidePage(Button page) => page.gameObject.SetActive(false);

    // ---------- fade ----------

    public IEnumerator FadeToBlack(float duration = 0.6f)
    {
        fade.blocksRaycasts = true;
        yield return UIAnim.Fade(fade, 1f, duration);
    }

    public IEnumerator FadeFromBlack(float duration = 0.8f)
    {
        fade.blocksRaycasts = false; // clickable as soon as it starts fading in (the title ignored clicks before)
        yield return UIAnim.Fade(fade, 0f, duration);
    }

    // ---------- day banner ----------

    public IEnumerator ShowDayBanner(int day)
    {
        dayBannerLabel.text = "DAY " + day;
        yield return UIAnim.Fade(dayBanner, 1f, 0.4f);
        yield return new WaitForSecondsRealtime(1.4f);
        yield return UIAnim.Fade(dayBanner, 0f, 0.6f);
    }

    // ---------- video ----------

    /// Plays the transition video full-screen. Ends on a black screen. Falls back to a plain fade if the video fails.
    public IEnumerator PlayTransition()
    {
        yield return FadeToBlack(0.5f);
        if (video == null) yield break;

        if (videoTexture == null)
        {
            videoTexture = new RenderTexture(1920, 1080, 0);
            video.targetTexture = videoTexture;
            videoImage.texture = videoTexture;
        }
        video.source = VideoSource.Url;
        video.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoFile);
        bool failed = false;
        VideoPlayer.ErrorEventHandler onError = (vp, msg) => { failed = true; Debug.LogWarning("Transition video failed: " + msg); };
        video.errorReceived += onError;
        video.Prepare();
        float timeout = 5f;
        while (!video.isPrepared && !failed && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }

        if (!failed && video.isPrepared)
        {
            videoImage.gameObject.SetActive(true);
            video.Play();
            yield return null;
            fade.alpha = 0f; // video now covers the screen
            double length = video.length > 0 ? video.length : 3.0;
            float t = 0f;
            while (t < length + 0.2f && (video.isPlaying || t < 0.5f))
            {
                if (t > 0.4f && GameInput.ClickedThisFrame) break; // click to skip the bedtime video
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            fade.alpha = 1f;
            video.Stop();
            videoImage.gameObject.SetActive(false);
        }
        video.errorReceived -= onError;
    }
}
