using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// The BuyMe flyer that slides in once per day, by chance.
/// Chance = 15% on Day 1, +10% per day. From Day 4 clicking it opens the shop; before that it just dismisses.
public class FlyerPopup : MonoBehaviour
{
    [Header("Rules")]
    [Range(0f, 1f)] public float baseChance = 0.15f;
    [Range(0f, 1f)] public float chancePerDay = 0.10f;
    public int opensShopFromDay = 4;
    public Vector2 delayRange = new Vector2(12f, 40f);

    [Header("Refs")]
    [SerializeField] RectTransform card;
    [SerializeField] Button flyerButton, closeButton;
    [SerializeField] Shop shop;
    [SerializeField] AudioClip popSound;

    public bool IsOpen => card.gameObject.activeSelf;

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
        float chance = Mathf.Clamp01(baseChance + chancePerDay * (day - 1));
        if (Random.value < chance) pending = StartCoroutine(ShowAfterDelay(Random.Range(delayRange.x, delayRange.y)));
    }

    public void Cancel()
    {
        if (pending != null) StopCoroutine(pending);
        pending = null;
        Hide();
    }

    IEnumerator ShowAfterDelay(float delay)
    {
        // Wait out the delay only while the player is actually free to look around.
        while (delay > 0f)
        {
            if (GameManager.Instance == null || GameManager.Instance.WorldInputEnabled) delay -= Time.deltaTime;
            yield return null;
        }
        pending = null;
        Show();
    }

    public void Show()
    {
        card.gameObject.SetActive(true);
        Sfx.Play(popSound, 0.7f);
        StartCoroutine(UIAnim.SlideY(card, shownY - 1400f, shownY, 0.5f));
    }

    public void Hide() => card.gameObject.SetActive(false);

    void OnFlyerClicked()
    {
        Hide();
        if (day >= opensShopFromDay && shop != null) shop.Open();
    }
}
