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

    public ShopItem Item { get; private set; }
    public int Price { get; private set; }

    public void Bind(ShopItem item, int price)
    {
        Item = item;
        Price = price;
        group.alpha = item != null ? 1f : 0f;
        group.blocksRaycasts = item != null;
        if (item == null) return;

        icon.sprite = item.Icon;
        icon.preserveAspect = true;
        nameLabel.text = item.displayName;
        bool owned = item.Owned;
        ownedBadge.SetActive(owned);
        icon.color = owned ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        buyButton.gameObject.SetActive(!owned);
    }
}
