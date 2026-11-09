using UnityEngine;
using UnityEngine.UI;

/// One of the four product cards on the BuyMe page.
public class ShopCard : MonoBehaviour
{
    public Image icon;
    public Text nameLabel;
    public GameObject ownedBadge;
    public Button buyButton;
    public CanvasGroup group;
    [Tooltip("Red sticker showing the real price when a price hike is on (the card art prints the old one).")]
    public GameObject priceSticker;
    public Text priceStickerLabel;

    public ShopItem Item { get; private set; }
    public int Price { get; private set; }

    public void Bind(ShopItem item, int price, int printedPrice = -1)
    {
        Item = item;
        Price = price;
        group.alpha = item != null ? 1f : 0f;
        group.blocksRaycasts = item != null;
        if (item == null) { if (priceSticker != null) priceSticker.SetActive(false); return; }

        icon.sprite = item.Icon;
        icon.preserveAspect = true;
        nameLabel.text = item.displayName;
        bool owned = item.Owned;
        ownedBadge.SetActive(owned);
        icon.color = owned ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        buyButton.gameObject.SetActive(!owned);
        if (priceSticker != null)
        {
            bool hiked = !owned && printedPrice > 0 && price != printedPrice;
            priceSticker.SetActive(hiked);
            if (hiked) priceStickerLabel.text = "NOW " + price + "!";
        }
    }
}
