using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// The BuyMe flyer: guaranteed once within ~15 s on Days 1-3, then by chance up to 2-3 times a day.
/// From Day 4: chance = 15% + 10% per day for each roll. From Day 4 clicking it opens the shop; before that it just dismisses.
/// From Day 2 it may arrive in its anomaly version: clicking that one counts as a fixed anomaly instead.
public class FlyerPopup : MonoBehaviour
{
    [Header("Rules")]
    [Range(0f, 1f)] public float baseChance = 0.15f;
    [Range(0f, 1f)] public float chancePerDay = 0.10f;
    public int opensShopFromDay = 4;
    public Vector2 delayRange = new Vector2(12f, 40f);
    [Tooltip("Up to this day, one flyer is guaranteed early in the day.")]
    public int guaranteedUntilDay = 3;
    [Tooltip("Seconds of free play before the guaranteed flyer appears.")]
    public Vector2 guaranteedDelay = new Vector2(5f, 15f);
    [Tooltip("From this day the flyer gets several chances per day: day - 2 of them, up to maxRollsPerDay.")]
    public int repeatFromDay = 4;
    public int maxRollsPerDay = 3;

    [Header("Anomaly version")]
    public int anomalyFromDay = 2;
    [Range(0f, 1f)] public float anomalyChance = 0.3f;
    [Range(0f, 1f)] public float anomalyChancePerDay = 0.1f;
    [Tooltip("Layers shown on the normal flyer (normal truck, normal logo).")]
    public GameObject[] normalLayers;
    [Tooltip("Layers shown on the anomaly flyer (eyeball truck, eyed logo).")]
    public GameObject[] anomalyLayers;
    public AudioClip fixedSound;

    [Header("Refs")]
    [SerializeField] RectTransform card;
    [SerializeField] Button flyerButton, closeButton;
    [SerializeField] Shop shop;
    [SerializeField] AudioClip popSound;

    public bool IsOpen => card.gameObject.activeSelf;
    public bool IsAnomalous { get; private set; }

    int day;
    Coroutine pending;
    float shownY;

    void Awake()
    {
        shownY = card.anchoredPosition.y;
        flyerButton.onClick.AddListener(OnFlyerClicked);
        closeButton.onClick.AddListener(Hide);
        card.gameObject.SetActive(false);
    }

    public void OnDayStart(int newDay)
    {
        day = newDay;
        Cancel();
        if (day <= guaranteedUntilDay) pending = StartCoroutine(Guaranteed());
        else
        {
            int rolls = day >= repeatFromDay ? Mathf.Min(maxRollsPerDay, day - 2) : 1;
            pending = StartCoroutine(Rolls(rolls));
        }
    }

    IEnumerator Guaranteed()
    {
        yield return WaitFreeTime(Random.Range(guaranteedDelay.x, guaranteedDelay.y));
        Show();
        pending = null;
    }

    /// Each roll: wait a while, then show with the day's chance. Later rolls wait until the previous flyer is gone.
    IEnumerator Rolls(int rolls)
    {
        float chance = Mathf.Clamp01(baseChance + chancePerDay * (day - 1));
        for (int i = 0; i < rolls; i++)
        {
            yield return WaitFreeTime(Random.Range(delayRange.x, delayRange.y));
            if (Random.value < chance) Show();
            while (IsOpen) yield return null;
        }
        pending = null;
    }

    public void Cancel()
    {
        if (pending != null) StopCoroutine(pending);
        pending = null;
        Hide();
    }

    /// Counts down only while the player is actually free to look around.
    IEnumerator WaitFreeTime(float delay)
    {
        while (delay > 0f)
        {
            if (GameManager.Instance == null || GameManager.Instance.WorldInputEnabled) delay -= Time.deltaTime;
            yield return null;
        }
    }

    public void Show()
    {
        float chance = Mathf.Clamp01(anomalyChance + anomalyChancePerDay * (day - anomalyFromDay));
        IsAnomalous = day >= anomalyFromDay && Random.value < chance;
        foreach (var g in normalLayers) if (g != null) g.SetActive(!IsAnomalous);
        foreach (var g in anomalyLayers) if (g != null) g.SetActive(IsAnomalous);
        card.gameObject.SetActive(true);
        Sfx.Play(popSound, 0.7f);
        StartCoroutine(UIAnim.SlideY(card, shownY - 1400f, shownY, 0.5f));
    }

    public void Hide() => card.gameObject.SetActive(false);

    void OnFlyerClicked()
    {
        Hide();
        if (IsAnomalous)
        {
            // Spotting the wrong flyer counts as fixing an anomaly (hidden point) and never opens the shop.
            PlayerState.EarnPoint();
            Sfx.Play(fixedSound, 0.7f, 0.75f);
            return;
        }
        if (day >= opensShopFromDay && shop != null) shop.Open();
    }
}
