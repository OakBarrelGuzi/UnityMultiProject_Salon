using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Firebase.Database;

public class Inventory : MonoBehaviour
{
    public List<Socket> socketList = new List<Socket>();

    public Item itemPrefab;

    [SerializeField]
    private Transform emojiInven;
    [SerializeField]
    private Transform anumeInven;

    public void AddInventoryItem(ItemData itemData)
    {
        Item invenitem = null;
        if (itemData.itemType == ItemType.Emoji)
        {
            invenitem = Instantiate(itemPrefab, emojiInven.transform);
        }
        else if (itemData.itemType == ItemType.Anime)
        {
            invenitem = Instantiate(itemPrefab, anumeInven.transform);
        }
        if (invenitem != null)
        {
            invenitem.Initialize(itemData);
        }
    }

    public void AddItemData(ItemData itemData)
    {
        ItemData data = new ItemData()
        {
            itemCost = itemData.itemCost,
            itemName = itemData.itemName,
            itemType = itemData.itemType,
        };
        AddInventoryItem(data);
    }

    public void SocketItemDataAdd(ItemData itemData)
    {
        List<Socket> sockets = socketList.FindAll(socket => socket.socketType == itemData.itemType);

        if (sockets == null) return;
        Socket target = sockets.Find(socket => socket.itemData == null);
        if (target != null)
        {
            target.AddSocketItem(itemData);
        }
        else
        {
            AddItemData(itemData);
        }
    }
}

