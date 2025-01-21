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
    private string curChanel;
    public override void Open()
    {
        base.Open();
        Initialize();
        LoadRoomList(curChanel); // ���� ä�� ID�� �� ��� �ε�
    }

    public override void Initialize()
    {    
        createRoomButton.onClick.RemoveAllListeners();
        createRoomButton.onClick.AddListener(OnCreateRoomClick);

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClick);

        playerInfo = FirebaseManager.Instance.GetCurrentDisplayName();
        curChanel = ChannelManager.Instance.CurrentChannel;
    }

    public async void LoadRoomList(string channelId)
    {
        try
        {
            // �� ��� �ʱ�ȭ
            if (roomListContent != null)
            {
                foreach (Transform child in roomListContent)
                {
                    if (child != null && child.gameObject.scene.IsValid()) // Scene�� �ε�� ������Ʈ�� ����
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            // �� ��� ��������
            var roomIds = await GameRoomManager.Instance.GetRoomList(channelId);

            // �� ����� ��� �ִ� ��� ó��
            if (roomIds == null || roomIds.Count == 0)
            {
                Debug.Log($"[LoadRoomList] ä�� {channelId}�� ���� �����ϴ�.");
                return;
            }

            // �� ��� ����
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
            Debug.LogError($"[LoadRoomList] �� ��� �ε� �� ���� �߻�: {ex.Message}");
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
            Debug.LogError("�� �̸��� �Է��ϼ���.");
            return;
        }

        if (string.IsNullOrEmpty(curChanel))
        {
            Debug.LogError("ä�� ID�� �������� �ʾҽ��ϴ�.");
            return;
        }

        string roomId = await GameRoomManager.Instance.CreateRoom(curChanel, playerInfo);
        if (!string.IsNullOrEmpty(roomId))
        {
            Debug.Log($"[RoomCreationUI] �� ���� ����: {roomName} (Room ID: {roomId})");
            roomNameInput.text = "";
            LoadRoomList(curChanel);
        }
        else
        {
            Debug.LogError("�� ������ �����߽��ϴ�. �ٽ� �õ��ϼ���.");
        }
    }
    public void OnCloseClick()
    {
        createRoomButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }
}
