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
    private DatabaseReference ItemRef;
    public List<Item> ItemList { get; private set; }

    public void Initialize()
    {
        ItemRef = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Inventory");
        ItemList = new List<Item>();
    }

    public Item AddItem(string itemName)
    {
        foreach (var item in ItemList)
        {
            if (item.itemData.itemName == itemName)
            {
                return item;
            }
        }
        return null;
    }

    public List<Item> GetAllItem()
    {
        return ItemList;
    }

    public async Task<UserInventory> LoadPlayerInventory()
    {
        var snapshot = await ItemRef.GetValueAsync();
        var inventory = JsonConvert.DeserializeObject<UserInventory>(snapshot.GetRawJsonValue());
        return inventory;
    }

    public async Task<bool> SavePlayerInventory(UserInventory inventory)
    {
        var json = JsonConvert.SerializeObject(inventory);
        await ItemRef.SetRawJsonValueAsync(json);
        return true;
    }

    public Sprite GetItemSprite(string itemName)
    {
        return Resources.Load<Sprite>("Sprites/Items/" + itemName);
    }

    public Sprite GetEmojiSprite(string emojiName)
    {
        return Resources.Load<Sprite>("Sprites/Emojis/" + emojiName);
    }

    public AnimationClip GetAnimeClip(string animName)
    {
        return Resources.Load<AnimationClip>("Animations/Animes/" + animName);
    }
}
