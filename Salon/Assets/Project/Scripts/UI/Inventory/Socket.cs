using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Salon.Firebase.Database;
using Salon.Character;
using System;
using System.Threading.Tasks;

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

    public async void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent<Item>(out Item item))
        {
            if (itemType != item.itemData.itemType) return;

            // 아이템 데이터 미리 캐시
            ItemData newItemData = new ItemData
            {
                itemType = item.itemData.itemType,
                itemName = item.itemData.itemName,
                itemCost = item.itemData.itemCost,
                socketIndex = transform.GetSiblingIndex()
            };
            Sprite itemSprite = item.itemIcon.sprite;

            // 현재 소켓의 아이템이 있다면 인벤토리로 되돌리기
            if (itemData != null)
            {
                var inventory = await ItemManager.Instance.LoadPlayerInventory();
                inventory.Items.Add(itemData);
                await ItemManager.Instance.SavePlayerInventory(inventory);

                // ActivatedItems에서 현재 아이템 제거
                var activatedItems = await ItemManager.Instance.LoadPlayerActivatedItems();
                activatedItems.Items.RemoveAll(x =>
                    x.itemName == itemData.itemName &&
                    x.itemType == itemData.itemType);
                await ItemManager.Instance.SavePlayerActivatedItems(activatedItems);
            }

            // 새 아이템의 UI 설정
            this.image.sprite = itemSprite;
            SetImageOpacity(1);

            // 새 아이템 데이터 설정
            itemData = newItemData;

            // 인벤토리에서 새 아이템 제거
            var newInventory = await ItemManager.Instance.LoadPlayerInventory();
            newInventory.Items.RemoveAll(x =>
                x.itemName == newItemData.itemName &&
                x.itemType == newItemData.itemType);
            await ItemManager.Instance.SavePlayerInventory(newInventory);

            // ActivatedItems에 새 아이템 추가
            var newActivatedItems = await ItemManager.Instance.LoadPlayerActivatedItems();
            newActivatedItems.Items.Add(newItemData);
            await ItemManager.Instance.SavePlayerActivatedItems(newActivatedItems);

            // 모든 작업이 완료된 후 드래그된 아이템 UI 제거
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
    }

    public void AddSocketItem(ItemData data)
    {
        if (itemType != data.itemType) return;

        itemData = new ItemData()
        {
            itemName = data.itemName,
            itemCost = data.itemCost,
            itemType = data.itemType,
        };

        image.sprite = ItemManager.Instance.GetItemSprite(data.itemName);
        SetImageOpacity(1);
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