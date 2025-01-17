using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Salon.Firebase;
using TMPro;

public class RoomCreationUI : Panel
{
    [Header("UI Elements")]
    public TMP_InputField roomNameInput;
    public Button createRoomButton;
    public Button closeButton;
    public Text errorMessage;

    [Header("Scroll View")]
    public Transform roomListContent;
    public GameObject roomItemPrefab;

    private string currentChannelId;
    public override void Open()
    {
        base.Open();
        Initialize();
    }

    private void Initialize()
    {
        
    }

    public void SetCurrentChannel(string channelId)
    {
        currentChannelId = channelId;
        LoadRoomList(); // 채널에 따라 방 목록 로드
    }
    public async void LoadRoomList()
    {
        // 기존 리스트 초기화
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // GameRoomManager에서 방 목록 가져오기
        var roomIds = await GameRoomManager.Instance.GetRoomList(currentChannelId);

        // 방 아이템 생성
        foreach (var roomId in roomIds)
        {
            GameObject roomItem = Instantiate(roomItemPrefab, roomListContent);
            var roomItemUI = roomItem.GetComponent<RoomItemUI>();

            if (roomItemUI != null)
            {
                roomItemUI.SetRoomInfo(roomId, OnJoinRoomClick);
            }
        }
    }
    private void OnJoinRoomClick(string roomId)
    {
        Debug.Log($"Joining room: {roomId}");

        GameRoomManager.Instance.JoinRoom(currentChannelId, roomId, "PlayerID", "PlayerName");
    }
    public async void OnCreateRoomClick()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            ShowError("방 이름을 입력하세요.");
            return;
        }

        if (string.IsNullOrEmpty(currentChannelId))
        {
            ShowError("채널 ID를 찾을 수 없습니다.");
            return;
        }

        string roomId = await GameRoomManager.Instance.CreateRoomInChannel(currentChannelId, roomName);
        if (!string.IsNullOrEmpty(roomId))
        {
            Debug.Log($"[RoomCreationUI] 방 생성 성공: {roomName} (Room ID: {roomId})");
            roomNameInput.text = "";
            LoadRoomList();
        }
        else
        {
            ShowError("방 생성에 실패했습니다. 다시 시도하세요.");
        }
    }

    public void OnCancelClicked()
    {
        createRoomButton.onClick.RemoveAllListeners();
        //rankingButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }

    private void ShowError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }
}
