using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// The BuyMe computer shop: owns the item list, the shop window and the delivery boxes.
public class Shop : MonoBehaviour
{
    [Header("Rules")]
    public int maxPurchasesPerDay = 4;

    [Header("Catalogue (display order, 4 per page)")]
    public ShopItem[] items;

    [Header("Window")]
    public GameObject window;
    public ShopCard[] cards;
    public Button closeButton, prevButton, nextButton;
    public Text cartLabel;
    public Toast toast;

    [Header("Room")]
    [Tooltip("Boxes that appear in the hallway, one per purchase.")]
    public GameObject[] deliveryBoxes;

    [Header("Sounds")]
    public AudioClip openSound, buySound, denySound;

    public bool IsOpen => window != null && window.activeSelf;
    public bool AllOwned => items.All(i => i == null || i.Owned);
    public int PageCount => Mathf.Max(1, Mathf.CeilToInt(items.Length / (float)cards.Length));

    int page;
    int boxesShown;
    readonly List<ShopItem> pendingReveal = new List<ShopItem>();

    void Awake()
    {
        closeButton.onClick.AddListener(Close);
        prevButton.onClick.AddListener(() => Turn(-1));
        nextButton.onClick.AddListener(() => Turn(+1));
        foreach (var card in cards)
        {
            var c = card;
            c.buyButton.onClick.AddListener(() => TryBuy(c));
        }
        window.SetActive(false);
    }

    /// Start of a run: hide everything that isn't owned.
    public void ResetRoom()
    {
        foreach (var item in items) if (item != null) item.gameObject.SetActive(item.Owned);
        boxesShown = 0;
        foreach (var b in deliveryBoxes) if (b != null) b.SetActive(false);
        pendingReveal.Clear();
    }

    public void Open()
    {
        if (IsOpen) return;
        page = 0;
        Refresh();
        window.SetActive(true);
        Sfx.Play(openSound, 0.6f);
        StartCoroutine(UIAnim.PopIn(window.transform, 0.92f, 0.2f));
    }

    public void Close()
    {
        if (!IsOpen) return;
        window.SetActive(false);
        StartCoroutine(RevealPending());
    }

    void Turn(int dir)
    {
        page = (page + dir + PageCount) % PageCount;
        Refresh();
        foreach (var c in cards) StartCoroutine(UIAnim.PopIn(c.transform, 0.9f, 0.18f));
    }

    void Refresh()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            int index = page * cards.Length + i;
            cards[i].Bind(index < items.Length ? items[index] : null);
        }
        cartLabel.text = PlayerState.PurchasesToday > 0 ? $"({PlayerState.PurchasesToday})" : "";
        prevButton.interactable = nextButton.interactable = PageCount > 1;
    }

    void TryBuy(ShopCard card)
    {
        var item = card.Item;
        if (item == null || item.Owned) return;

        if (PlayerState.PurchasesToday >= maxPurchasesPerDay)
        {
            Deny(card, "CART FULL");
            return;
        }
        if (!PlayerState.TryBuy(item.id, item.price))
        {
            Deny(card, "CAN'T AFFORD");
            return;
        }
        Sfx.Play(buySound, 0.7f);
        pendingReveal.Add(item);
        Refresh();
        StartCoroutine(UIAnim.PopIn(card.transform, 1.15f, 0.25f));
    }

    void Deny(ShopCard card, string message)
    {
        Sfx.Play(denySound, 0.6f, 0.8f);
        toast.Show(message);
        StartCoroutine(UIAnim.Shake((RectTransform)card.transform, 12f, 0.3f));
    }

    /// Bought items pop into the room once the window closes, so the player sees it happen.
    IEnumerator RevealPending()
    {
        var reveal = pendingReveal.ToArray();
        pendingReveal.Clear();
        foreach (var item in reveal)
        {
            item.Reveal();
            if (boxesShown < deliveryBoxes.Length && deliveryBoxes[boxesShown] != null)
            {
                var box = deliveryBoxes[boxesShown++];
                box.SetActive(true);
                var prop = box.GetComponent<Prop>();
                if (prop != null) prop.Pop(0f);
            }
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }
}
