using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Firebase.Database;
using Salon.System;

public class ItemManager : Singleton<ItemManager>
{
    public List<Item> ItemList { get; private set; }

    public void Initialize()
    {
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
}
