using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Firebase.Database;
using Salon.System;
using Salon.Firebase;
using Firebase.Database;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class ItemManager : Singleton<ItemManager>
{
    private DatabaseReference inventoryRef;
    private DatabaseReference activatedItemRef;
    public List<ItemData> ItemList { get; private set; }

    public async void Initialize()
    {
        inventoryRef = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Inventory");
        activatedItemRef = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("ActivatedItems");

        // ActivatedItems 노드가 없으면 생성
        var snapshot = await activatedItemRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            var newActivatedItems = new ActivatedItems { Items = new List<ItemData>() };
            string json = JsonConvert.SerializeObject(newActivatedItems);
            await activatedItemRef.SetRawJsonValueAsync(json);
        }

        ItemList = new List<ItemData>();
        LoadItemDataFromCSV();
    }

    private void LoadItemDataFromCSV()
    {
        string csvPath = "Assets/Resources/Data/ItemData.csv";
        CSVManager.Instance.LoadAdditionalCSV(csvPath);
        var csvData = CSVManager.Instance.GetDataSet(csvPath);

        for (int i = 1; i < csvData.Count; i++)
        {
            ItemData item = new ItemData
            {
                itemName = csvData[i][0],
                itemType = (ItemType)System.Enum.Parse(typeof(ItemType), csvData[i][1]),
                itemCost = int.Parse(csvData[i][2])
            };
            ItemList.Add(item);
        }
    }

    public List<ItemData> GetAllItem()
    {
        return ItemList;
    }

    public async Task<UserInventory> LoadPlayerInventory()
    {
        var snapshot = await inventoryRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            // 인벤토리 노드가 없으면 새로 생성
            var newInventory = new UserInventory { Items = new List<ItemData>() };
            await SavePlayerInventory(newInventory);
            return newInventory;
        }
        var inventory = JsonConvert.DeserializeObject<UserInventory>(snapshot.GetRawJsonValue());
        return inventory ?? new UserInventory { Items = new List<ItemData>() };
    }

    public async Task RemoveInventoryItem(ItemData itemData)
    {
        Debug.Log($"Removing item {itemData.itemName} from inventory");
        var inventory = await LoadPlayerInventory();
        if (inventory != null)
        {
            // 같은 이름과 타입을 가진 아이템을 찾아서 제거
            int removedCount = inventory.Items.RemoveAll(x =>
                x.itemName == itemData.itemName &&
                x.itemType == itemData.itemType);

            Debug.Log($"Removed {removedCount} items from inventory");

            // 변경된 인벤토리 저장
            await SavePlayerInventory(inventory);
            Debug.Log("Updated inventory saved to Firebase");
        }
    }

    public async Task<ActivatedItems> LoadPlayerActivatedItems()
    {
        var snapshot = await activatedItemRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            // 활성화된 아이템 노드가 없으면 새로 생성
            var newActivatedItems = new ActivatedItems { Items = new List<ItemData>() };
            await SavePlayerActivatedItems(newActivatedItems);
            return newActivatedItems;
        }
        var activatedItems = JsonConvert.DeserializeObject<ActivatedItems>(snapshot.GetRawJsonValue());
        return activatedItems ?? new ActivatedItems { Items = new List<ItemData>() };
    }

    public async Task<bool> SavePlayerInventory(UserInventory inventory)
    {
        var json = JsonConvert.SerializeObject(inventory);
        await inventoryRef.SetRawJsonValueAsync(json);
        return true;
    }

    public async Task<bool> SavePlayerActivatedItems(ActivatedItems activatedItems)
    {
        var json = JsonConvert.SerializeObject(activatedItems);
        await activatedItemRef.SetRawJsonValueAsync(json);
        return true;
    }

    public Sprite GetItemSprite(string itemName)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/Items/" + itemName);
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>("Sprites/Items/DefaultIcon");
            if (sprite == null)
            {
                Debug.LogError("기본 아이콘(DefaultIcon)을 찾을 수 없습니다.");
            }
        }
        return sprite;
    }

    public Sprite GetEmojiSprite(string emojiName)
    {
        Sprite sprite = Resources.Load<Sprite>("Sprites/Emojis/" + emojiName);
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>("Sprites/Items/DefaultIcon");
            if (sprite == null)
            {
                Debug.LogError("기본 아이콘(DefaultIcon)을 찾을 수 없습니다.");
            }
        }
        return sprite;
    }

    public AnimationClip GetAnimClip(string animName)
    {
        return Resources.Load<AnimationClip>("Animations/" + animName);
    }
}
