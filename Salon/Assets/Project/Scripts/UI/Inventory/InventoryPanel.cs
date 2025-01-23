using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Firebase.Database;
using Salon.Character;
using UnityEngine.UI;
using System.Threading.Tasks;

public class InventoryPanel : Panel
{

    public Item itemPrefab;
    public Transform emojiInven;
    public Transform animInven;

    [SerializeField]
    private Button emojiButton;
    [SerializeField]
    private Button animButton;
    [SerializeField]
    private GameObject emojiPopupPanel;
    [SerializeField]
    private GameObject animPopupPanel;
    [SerializeField]
    private Button closeButton;

    private List<Item> spawnedItems = new List<Item>();

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override async void Initialize()
    {
        InitializeUI();
        ItemManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        await LoadInventoryItems();
    }

    public override void Close()
    {
        base.Close();
        ItemManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (gameObject.activeInHierarchy)
        {
            RefreshInventory();
        }
    }

    private void InitializeUI()
    {
        closeButton.onClick.AddListener(Close);

        emojiButton.onClick.AddListener(() =>
        {
            emojiPopupPanel.SetActive(true);
            animPopupPanel.SetActive(false);
            emojiInven.gameObject.SetActive(true);
            animInven.gameObject.SetActive(false);
        });

        animButton.onClick.AddListener(() =>
        {
            animPopupPanel.SetActive(true);
            emojiPopupPanel.SetActive(false);
            animInven.gameObject.SetActive(true);
            emojiInven.gameObject.SetActive(false);
        });

        // 기본값으로 이모지 패널 활성화
        emojiPopupPanel.SetActive(true);
        animPopupPanel.SetActive(false);
        emojiInven.gameObject.SetActive(true);
        animInven.gameObject.SetActive(false);
    }

    private async Task LoadInventoryItems()
    {
        // 기존 아이템 제거
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        spawnedItems.Clear();

        // 새로운 아이템 로드
        UserInventory inventory = await ItemManager.Instance.LoadPlayerInventory();
        foreach (var itemData in inventory.Items)
        {
            AddInventoryItem(itemData);
        }
    }

    public void AddInventoryItem(ItemData itemData)
    {
        Transform parent = (itemData.itemType == ItemType.Emoji) ? emojiInven : animInven;
        Item invenItem = Instantiate(itemPrefab, parent);
        invenItem.Initialize(itemData);
        spawnedItems.Add(invenItem);
    }

    public void RefreshInventory()
    {
        LoadInventoryItems().ConfigureAwait(false);
    }
}
