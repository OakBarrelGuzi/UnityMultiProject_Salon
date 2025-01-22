using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Salon.Firebase;
using System.Threading.Tasks;
using Salon.Firebase.Database;
public class ShopPanel : MonoBehaviour
{
    public Transform itemParent;
    public List<Item> itemList;
    public Item itemPrefab;

    public void Initialize()
    {
        itemList = ItemManager.Instance.GetAllItem();
        foreach (var item in itemList)
        {
            Item itemObj = Instantiate(itemPrefab, itemParent);
            itemObj.Initialize(item.itemData);
            Button button = itemObj.GetComponent<Button>();
            button.onClick.AddListener(() => BuyItem(itemObj));

        }
    }

    public async void AddItemToPlayerInventory(Item item)
    {
        if (ItemManager.Instance.AddItem(item.itemData.itemName))
        {
            await ItemManager.Instance.SavePlayerInventory(await ItemManager.Instance.LoadPlayerInventory());
        }
    }

    public async void BuyItem(Item item)
    {
        if (await GetPlayerGold() >= item.itemData.itemCost)
        {
            UpdatePlayerGold(await GetPlayerGold() - item.itemData.itemCost);
            AddItemToPlayerInventory(item);
        }
    }

    public async Task<int> GetPlayerGold()
    {
        var Ref = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Gold");
        var snapshot = await Ref.GetValueAsync();
        return int.Parse(snapshot.GetRawJsonValue());
    }

    public async void UpdatePlayerGold(int gold)
    {
        var Ref = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Gold");
        await Ref.SetValueAsync(gold);
    }
}
