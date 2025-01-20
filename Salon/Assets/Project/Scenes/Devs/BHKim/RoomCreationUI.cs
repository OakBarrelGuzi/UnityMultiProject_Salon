using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Salon.Firebase;
using TMPro;
using System;

public class RoomCreationUI : Panel
{
    [Header("UI Elements")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public Button closeButton;

    [Header("Scroll View")]
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    private string playerInfo = "PlayerID";
    private string currentChannel;
    public override void Open()
    {
        base.Open();
        Initialize();
        LoadRoomList(currentChannel); // 현재 채널 ID로 방 목록 로드
    }

    public override void Initialize()
    {
        createRoomButton.onClick.RemoveAllListeners();
        createRoomButton.onClick.AddListener(OnCreateRoomClick);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClick);

        playerInfo = FirebaseManager.Instance.GetCurrentDisplayName();
        currentChannel = ChannelManager.Instance.CurrentChannel;
    }

    public async void LoadRoomList(string channelId)
    {
        try
        {
            // 방 목록 초기화
            if (roomListContent != null)
            {
                foreach (Transform child in roomListContent)
                {
                    if (child != null && child.gameObject.scene.IsValid()) // Scene에 로드된 오브젝트만 삭제
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            // 방 목록 가져오기
            var roomIds = await GameRoomManager.Instance.GetRoomList(channelId);

            // 방 목록이 비어 있는 경우 처리
            if (roomIds == null || roomIds.Count == 0)
            {
                Debug.Log($"[LoadRoomList] 채널 {channelId}에 방이 없습니다.");
                return;
            }

            // 방 목록 생성
            foreach (var roomId in roomIds)
            {
                GameObject roomItem = Instantiate(roomItemPrefab, roomListContent.transform, false);
                var roomItemUI = roomItem.GetComponent<RoomItemUI>();

                if (roomItemUI != null)
                {
                    roomItemUI.SetRoomInfo(roomId, async (id) => await OnJoinRoomClick(channelId, id));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoadRoomList] 방 목록 로드 중 오류 발생: {ex.Message}");
        }
    }

    private async Task OnJoinRoomClick(string channelId, string roomId)
    {
        try
        {
            await GameRoomManager.Instance.JoinRoom(channelId, roomId, playerInfo);
            Debug.Log("Successfully joined the room!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to join the room: {ex.Message}");
        }
    }

    public async void OnCreateRoomClick()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("방 이름을 입력하세요.");
            return;
        }

        if (string.IsNullOrEmpty(currentChannel))
        {
            Debug.LogError("채널 ID를 설정하지 않았습니다.");
            return;
        }

        string roomId = await GameRoomManager.Instance.CreateRoom(currentChannel, playerInfo);
        if (!string.IsNullOrEmpty(roomId))
        {
            Debug.Log($"[RoomCreationUI] 방 생성 성공: {roomName} (Room ID: {roomId})");
            roomNameInput.text = "";
            LoadRoomList(currentChannel);
        }
        else
        {
            Debug.LogError("방 생성에 실패했습니다. 다시 시도하세요.");
        }
    }
    public void OnCloseClick()
    {
        createRoomButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }
}