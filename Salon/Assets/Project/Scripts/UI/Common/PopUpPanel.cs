using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Salon.Firebase.Database;

public class PopUpPanel : Panel
{
    [SerializeField] private Button yesButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text messageText;

    private string currentInviteKey;
    private string currentFriendRequest;
    private Action onAcceptAction;

    public override void Open()
    {
        base.Open();
        FriendManager.Instance.OnInviteReceived.AddListener(OnInviteReceived);
        FriendManager.Instance.OnFriendRequestReceived.AddListener(OnFriendRequestReceived);
    }

    public override void Close()
    {
        base.Close();
        FriendManager.Instance.OnInviteReceived.RemoveListener(OnInviteReceived);
        FriendManager.Instance.OnFriendRequestReceived.RemoveListener(OnFriendRequestReceived);

        if (!string.IsNullOrEmpty(currentInviteKey))
        {
            _ = FriendManager.Instance.RemoveInvite(currentInviteKey);
        }
        if (!string.IsNullOrEmpty(currentFriendRequest))
        {
            _ = FriendManager.Instance.DeclineFriendRequest(currentFriendRequest);
        }
    }

    public override void Initialize(Action acceptAction = null, string message = "")
    {
        onAcceptAction = acceptAction;
        if (!string.IsNullOrEmpty(message))
        {
            messageText.text = message;
        }

        yesButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() =>
        {
            onAcceptAction?.Invoke();
            Close();
        });
        closeButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(currentInviteKey))
            {
                _ = FriendManager.Instance.DeclineInvite(currentInviteKey);
            }
            if (!string.IsNullOrEmpty(currentFriendRequest))
            {
                _ = FriendManager.Instance.DeclineFriendRequest(currentFriendRequest);
            }
            Close();
        });
    }

    private void OnInviteReceived(string inviter, string channelName, string inviteKey)
    {
        currentInviteKey = inviteKey;
        currentFriendRequest = null;
        messageText.text = $"{inviter}님이 {channelName} 채널로 초대했습니다.";
    }

    private void OnFriendRequestReceived(string senderDisplayName)
    {
        currentFriendRequest = DisplayNameUtils.ToServerFormat(senderDisplayName);
        currentInviteKey = null;
        messageText.text = $"{senderDisplayName}님이 친구 요청을 보냈습니다.";
    }
}