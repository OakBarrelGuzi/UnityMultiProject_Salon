using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Salon.Firebase.Database;
using Salon.Character;
using System;

public class Socket : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public event Action<ItemData> OnItemChanged;
    public ItemData itemData { get; private set; }
    public ItemType itemType;
    private Image image;
    private Panel parentPanel;

    private void Start()
    {
        image = GetComponentInChildren<Image>();
        parentPanel = GetComponentInParent<Panel>();
        // 처음에는 이미지를 투명하게 설정
        SetImageOpacity(0);
    }

    private void SetImageOpacity(float opacity)
    {
        if (image != null)
        {
            Color color = image.color;
            color.a = opacity;
            image.color = color;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent<Item>(out Item item))
        {
            if (itemType != item.itemData.itemType) return;

            AddSocketItem(item);
            if (itemData != null)
            {
                OnItemChanged?.Invoke(itemData);
            }
        }
    }

    public void AddSocketItem(Item item)
    {
        if (itemType != item.itemData.itemType) return;
        this.image.sprite = item.itemIcon.sprite;
        SetImageOpacity(1); // 아이템이 추가될 때 이미지를 보이게 함

        ItemData data = new ItemData()
        {
            itemType = item.itemData.itemType,
            itemName = item.itemData.itemName,
            itemCost = item.itemData.itemCost,
        };

        if (itemData != null)
        {
            OnItemChanged?.Invoke(itemData);
        }

        itemData = data;
        Destroy(item.gameObject);
    }

    public void AddSocketItem(ItemData data)
    {
        if (itemType != data.itemType) return;

        ItemData item = new ItemData()
        {
            itemName = data.itemName,
            itemCost = data.itemCost,
            itemType = data.itemType,
        };
        itemData = item;
        image.sprite = ItemManager.Instance.GetItemSprite(data.itemName);
        SetImageOpacity(1); // 아이템이 추가될 때 이미지를 보이게 함
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData != null)
        {
            if (itemData.itemType == ItemType.Emoji)
            {
                GameManager.Instance.player.GetComponent<LocalPlayer>().animController.SetEmoji(itemData.itemName);
                parentPanel?.Close();
            }
            else if (itemData.itemType == ItemType.Anim)
            {
                GameManager.Instance.player.GetComponent<LocalPlayer>().animController.SetAnime(itemData.itemName);
                parentPanel?.Close();
            }
        }
    }

    public void ClearItem()
    {
        itemData = null;
        if (image != null)
        {
            SetImageOpacity(0);
        }
    }
}