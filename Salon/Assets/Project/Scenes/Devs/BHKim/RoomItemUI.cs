using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class RoomItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text roomNameText;
    public Button joinButton;

    private string roomId;
    private Action<string> onJoinCallback;

    public void SetRoomInfo(string roomId, Action<string> onJoinCallback)
    {
        this.roomId = roomId;
        this.onJoinCallback = onJoinCallback;

        roomNameText.text = roomId; // 방 이름 설정
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoinCallback(roomId));
    }
}
