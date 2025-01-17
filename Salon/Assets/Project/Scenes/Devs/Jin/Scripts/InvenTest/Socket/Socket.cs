using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Salon.Inven
{
    public class Socket : MonoBehaviour, IDropHandler
    {
        private Inventory inventory;

        private ItemData itemData;

        [SerializeField]
        private itemType socketType;

        [SerializeField]
        private Image image;

        private void Start()
        {
            inventory = GetComponentInParent<Inventory>();
            image = GetComponentInChildren<Image>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag.TryGetComponent<Item>(out Item item))
            {
                this.image.sprite = item.image.sprite;

                AddSocketItem(item);

                if (itemData == null) return;
                print($"{itemData.itemName}");
                print($"{itemData.itemType}");
                print($"{itemData.itemCost}");
            }
        }

        public void AddSocketItem(Item item)
        {
            if (socketType != item.itemData.itemType) return;

            if (itemData != null)
            {
                inventory.AddItemData(itemData);
            }
            ItemData data = new ItemData()
            {
                itemType = item.itemData.itemType,
                itemName = item.itemData.itemName,
                itemCost = item.itemData.itemCost,
            };
            itemData = data;
            Destroy(item.gameObject);
        }
    }
}
