using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salon.Firebase.Database;
using System.Threading.Tasks;

public class AnimPanel : Panel
{
    public List<Socket> socketList;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override async void Initialize()
    {
        // 모든 소켓의 타입을 Anim으로 설정
        foreach (var socket in socketList)
        {
            socket.itemType = ItemType.Anim;
        }

        await RefreshActivatedItems();
    }

    public async Task RefreshActivatedItems()
    {
        var activatedItems = await ItemManager.Instance.LoadPlayerActivatedItems();

        // 소켓 초기화 및 이벤트 연결
        foreach (var socket in socketList)
        {
            // 기존 이벤트 리스너 제거
            socket.OnItemChanged -= HandleSocketItemChanged;
            socket.OnItemChanged += HandleSocketItemChanged;

            // 소켓 초기화
            socket.ClearItem();

            // 활성화된 애니메이션 아이템 찾아서 소켓에 설정
            var activeItem = activatedItems.Items.Find(item =>
                item.itemType == ItemType.Anim &&
                item.socketIndex == socketList.IndexOf(socket));

            if (activeItem != null)
            {
                socket.AddSocketItem(activeItem);
            }
        }
    }

    private async void HandleSocketItemChanged(ItemData previousItem)
    {
        if (previousItem == null) return;

        // 이전 아이템을 인벤토리로 되돌림
        var inventory = await ItemManager.Instance.LoadPlayerInventory();
        inventory.Items.Add(previousItem);
        await ItemManager.Instance.SavePlayerInventory(inventory);

        // 활성화된 아이템 목록 업데이트
        var activatedItems = await ItemManager.Instance.LoadPlayerActivatedItems();
        activatedItems.Items.RemoveAll(item =>
            item.itemType == ItemType.Anim &&
            item.socketIndex == socketList.FindIndex(s => s.itemData?.itemName == item.itemName));

        // 새 아이템 추가
        foreach (var socket in socketList)
        {
            if (socket.itemData != null)
            {
                var newItem = new ItemData
                {
                    itemName = socket.itemData.itemName,
                    itemType = socket.itemData.itemType,
                    itemCost = socket.itemData.itemCost,
                    socketIndex = socketList.IndexOf(socket)
                };
                activatedItems.Items.Add(newItem);
            }
        }

        await ItemManager.Instance.SavePlayerActivatedItems(activatedItems);
    }
}
