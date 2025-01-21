using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Salon.Firebase;
using TMPro;
using System;

public class RoomCreationUI : Panel
{
    [Header("UI Elements")]
    public Button closeButton;
    public GameObject findOp;
    public GameObject matching;

    private string currentRoomId;
    private string currentChannelId;
    private string currentPlayerId;

    public override void Open()
    {
        base.Open();
        Initialize();
    }
    public void SetRoomData(string roomId, string channelId, string playerId)
    {
        currentRoomId = roomId;
        currentChannelId = channelId;
        currentPlayerId = playerId;
    }
    public override void Initialize()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClick);
        findOp.SetActive(false);
    }
    public void OnFind()
    {
        matching.SetActive(false);
        findOp.SetActive(true);
    }

    public async void OnCloseClick()
    {
        closeButton.onClick.RemoveAllListeners();

        if (!string.IsNullOrEmpty(currentRoomId) && !string.IsNullOrEmpty(currentChannelId))
        {
            Debug.Log($"[RoomCreationUI] 방 삭제 시도: RoomId({currentRoomId}), ChannelId({currentChannelId})");
            await GameRoomManager.Instance.DeleteRoom(currentChannelId, currentRoomId, currentPlayerId);
        }

        base.Close();
    }
}
