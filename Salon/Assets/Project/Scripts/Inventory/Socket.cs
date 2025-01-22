using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Salon.Firebase.Database;
using Salon.Character;

public class Socket : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    private Inventory inventory;

    public ItemData itemData { get; private set; }

    public ItemType itemType;

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

            AddSocketItem(item);

            if (itemData == null) return;
            print($"{itemData.itemName}");
            print($"{itemData.itemType}");
            print($"{itemData.itemCost}");
        }
    }

    public void AddSocketItem(Item item)
    {
        if (itemType != item.itemData.itemType) return;
        this.image.sprite = item.image.sprite;
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

    public void AddSocketItem(ItemData data)
    {
        ItemData item = new ItemData()
        {
            itemName = data.itemName,
            itemCost = data.itemCost,
            itemType = data.itemType,
        };
        itemData = item;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData != null)
        {
            if (itemData.itemType == ItemType.Emoji)
            {
                GameManager.Instance.player.GetComponent<LocalPlayer>().animController.SetEmoji(itemData.itemName);
            }
            else if (itemData.itemType == ItemType.Anime)
            {
                GameManager.Instance.player.GetComponent<LocalPlayer>().animController.SetAnime(itemData.itemName);
            }
        }
    }

}