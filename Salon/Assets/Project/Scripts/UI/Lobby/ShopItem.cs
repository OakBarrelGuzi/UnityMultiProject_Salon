using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Salon.Firebase.Database;
using Salon.UI;

public class ShopItem : MonoBehaviour
{
    public Image itemImage;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    private ItemData itemData;
    private ShopPanel shopPanel;

    public void Initialize(ItemData data, ShopPanel panel)
    {
        itemData = data;
        shopPanel = panel;

        itemImage.sprite = ItemManager.Instance.GetItemSprite(data.itemName);
        priceText.text = data.itemCost.ToString();

        buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    private async void OnBuyButtonClicked()
    {
        int currentGold = await shopPanel.GetPlayerGold();
        if (currentGold >= itemData.itemCost)
        {
            await shopPanel.BuyItem(itemData);
            Destroy(gameObject);
        }
        else
        {
            PopUpManager.Instance.ShowPopUp("골드가 부족합니다");
        }
    }
}