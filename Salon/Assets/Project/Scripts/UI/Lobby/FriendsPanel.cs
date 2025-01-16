using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Salon.Firebase;
using System.Threading.Tasks;
using Salon.Firebase.Database;

public class FriendsPanel : Panel
{
    [SerializeField] private FriendsInfoUI friendsInfoUIPrefab;
    [SerializeField] private Transform friendsInfoUIParent;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_InputField addFriendInputField;
    [SerializeField] private TMP_InputField inviteFriendInputField;
    [SerializeField] private Button sendFriendRequestButton;
    [SerializeField] private Button inviteFriendButton;

    private List<FriendsInfoUI> friendsInfoUIList = new List<FriendsInfoUI>();

    public override void Open()
    {
        base.Open();
        Initialize();
        LoadFriendsList();
    }

    public override void Close()
    {
        base.Close();
        ClearFriendsList();
    }

    public override void Initialize()
    {
        StartCoroutine(InitializeRoutine());
    }

    public IEnumerator InitializeRoutine()
    {
        yield return new WaitUntil(() => FriendManager.Instance.isInitialized);
        friendsInfoUIList.Clear();
        closeButton.onClick.AddListener(() => Close());
        sendFriendRequestButton.onClick.AddListener(OnSendFriendRequestButtonClick);
        inviteFriendButton.onClick.AddListener(OnInviteFriendButtonClick);
    }

    private void ClearFriendsList()
    {
        foreach (var friendUI in friendsInfoUIList)
        {
            if (friendUI != null)
            {
                Destroy(friendUI.gameObject);
            }
        }
        friendsInfoUIList.Clear();
    }

    private async void LoadFriendsList()
    {
        ClearFriendsList();
        var friends = await FriendManager.Instance.GetFriendsList();
        if (friends != null)
        {
            foreach (var friend in friends)
            {
                CreateFriendUI(friend.Key, friend.Value);
            }
        }
    }

    private void CreateFriendUI(string displayName, Salon.Firebase.Database.UserData userData)
    {
        var friendUI = Instantiate(friendsInfoUIPrefab, friendsInfoUIParent);
        friendUI.Initialize(displayName, userData);
        friendsInfoUIList.Add(friendUI);
    }

    private async void OnSendFriendRequestButtonClick()
    {
        sendFriendRequestButton.interactable = false;
        try
        {
            string friendId = addFriendInputField.text.Trim();
            if (string.IsNullOrEmpty(friendId))
            {
                LogManager.Instance.ShowLog("친구 ID를 입력해주세요.");
                return;
            }

            bool success = await FriendManager.Instance.SendFriendRequest(friendId);
            if (success)
            {
                addFriendInputField.text = "";
            }
        }
        finally
        {
            sendFriendRequestButton.interactable = true;
        }
    }

    private async void OnInviteFriendButtonClick()
    {
        string friendId = inviteFriendInputField.text.Trim();
        if (string.IsNullOrEmpty(friendId))
        {
            LogManager.Instance.ShowLog("초대할 친구의 ID를 입력해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(ChannelManager.Instance.CurrentChannel))
        {
            LogManager.Instance.ShowLog("채널에 입장한 후 초대해주세요.");
            return;
        }

        string serverFriendId = DisplayNameUtils.ToServerFormat(friendId);
        await InviteFriendToChannel(serverFriendId);
    }

    private async Task InviteFriendToChannel(string friendId)
    {
        try
        {
            await FriendManager.Instance.SendInvite(friendId, ChannelManager.Instance.CurrentChannel);
            inviteFriendInputField.text = "";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"친구 초대 실패: {ex.Message}");
            LogManager.Instance.ShowLog("친구 초대에 실패했습니다.");
        }
    }
}
