using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Salon.System;
using Salon.Firebase.Database;
using Salon.Firebase;
using System.Threading.Tasks;
using System.Linq;

public class ShopPanel : Panel
{
    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private Button emojiTabButton;
    [SerializeField]
    private Button animTabButton;

    public Transform emojiParent;
    public Transform animParent;
    public GameObject emojiPanel;
    public GameObject animPanel;
    public ShopItem shopItemPrefab;

    private List<ShopItem> spawnedItems = new List<ShopItem>();

    public override async void Initialize()
    {
        closeButton.onClick.AddListener(Close);
        emojiTabButton.onClick.AddListener(() => SwitchTab(true));
        animTabButton.onClick.AddListener(() => SwitchTab(false));

        // 기본값으로 이모지 탭 활성화
        SwitchTab(true);

        await RefreshShopItems();
    }

    private void SwitchTab(bool showEmoji)
    {
        emojiPanel.SetActive(showEmoji);
        animPanel.SetActive(!showEmoji);
        RefreshShopItems().ConfigureAwait(false);
    }

    private async Task RefreshShopItems()
    {
        // 기존 아이템 제거
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        spawnedItems.Clear();

        // 현재 보유한 아이템 목록 가져오기
        var inventory = await ItemManager.Instance.LoadPlayerInventory();
        var ownedItems = inventory.Items;

        // 상점 아이템 생성
        var allItems = ItemManager.Instance.GetAllItem();

        foreach (var itemData in allItems)
        {
            // 이미 보유한 아이템은 제외
            if (ownedItems.Any(item =>
                item.itemName == itemData.itemName &&
                item.itemType == itemData.itemType))
            {
                continue;
            }

            // 현재 탭에 맞는 아이템만 표시
            Transform parent = (itemData.itemType == ItemType.Emoji) ? emojiParent : animParent;
            if (!parent.gameObject.activeSelf) continue;

            ShopItem shopItem = Instantiate(shopItemPrefab, parent);
            shopItem.Initialize(itemData, this);
            spawnedItems.Add(shopItem);
        }
    }

    public async Task BuyItem(ItemData itemData)
    {
        int currentGold = await GetPlayerGold();
        if (currentGold >= itemData.itemCost)
        {
            await UpdatePlayerGold(currentGold - itemData.itemCost);
            await AddItemToPlayerInventory(itemData);
        }
    }

    private async Task AddItemToPlayerInventory(ItemData itemData)
    {
        var inventory = await ItemManager.Instance.LoadPlayerInventory();
        inventory.Items.Add(itemData);
        await ItemManager.Instance.SavePlayerInventory(inventory);
    }

    public async Task<int> GetPlayerGold()
    {
        var Ref = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Gold");
        var snapshot = await Ref.GetValueAsync();
        return int.Parse(snapshot.GetRawJsonValue());
    }

    private async Task UpdatePlayerGold(int gold)
    {
        var Ref = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Gold");
        await Ref.SetValueAsync(gold);
    }
}
