using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Salon.Inven
{
    public class Inventory : MonoBehaviour
    {
        public Item itemPrefab;

        [SerializeField]
        private Transform emojiInven;
        [SerializeField]
        private Transform anumeInven;

        public void AddInventoryItem(ItemData itemData)
        {
            Item invenitem = null;
            if (itemData.itemType == itemType.Emoji)
            {
                invenitem = Instantiate(itemPrefab, emojiInven.transform);
            }
            else if (itemData.itemType == itemType.Anime)
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

    }
}
