using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Salon.Firebase;
using Salon.Firebase.Database;
using System.Threading.Tasks;

public class FriendsInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private Button inviteFriendButton;
    [SerializeField] private GameObject onlineImage;
    [SerializeField] private GameObject offlineImage;

    private string friendDisplayName;
    private UserData userData;

    public void Initialize(string displayName, UserData data)
    {
        friendDisplayName = displayName;
        userData = data;
        displayNameText.text = displayName;

        UpdateOnlineStatus();
        SetupInviteButton();
    }

    private void UpdateOnlineStatus()
    {
        if (userData != null)
        {
            bool isOnline = userData.Status == UserStatus.Online;

            onlineImage.SetActive(isOnline);
            offlineImage.SetActive(!isOnline);

            inviteFriendButton.interactable = isOnline;

            switch (userData.Status)
            {
                case UserStatus.Online:
                    displayNameText.color = Color.green;
                    break;
                case UserStatus.Away:
                    displayNameText.color = Color.yellow;
                    break;
                case UserStatus.Busy:
                    displayNameText.color = Color.red;
                    break;
                case UserStatus.Offline:
                    displayNameText.color = Color.gray;
                    break;
            }
        }
        else
        {
            onlineImage.SetActive(false);
            offlineImage.SetActive(true);
            displayNameText.color = Color.gray;
        }
    }

    private void SetupInviteButton()
    {
        inviteFriendButton.onClick.RemoveAllListeners();
        inviteFriendButton.onClick.AddListener(OnInviteFriendButtonClick);
    }

    public void OnInviteFriendButtonClick()
    {
        if (string.IsNullOrEmpty(ChannelManager.Instance.CurrentChannel))
        {
            LogManager.Instance.ShowLog("채널에 입장한 후 초대해주세요.");
            return;
        }

        _ = InviteFriendToChannel();
    }

    private async Task InviteFriendToChannel()
    {
        try
        {
            if (string.IsNullOrEmpty(ChannelManager.Instance.CurrentChannel))
            {
                LogManager.Instance.ShowLog("채널에 입장한 후 초대해주세요.");
                return;
            }

            inviteFriendButton.interactable = false;
            await FriendManager.Instance.SendInvite(friendDisplayName);
            LogManager.Instance.ShowLog($"{friendDisplayName}님에게 채널 초대를 보냈습니다.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"친구 초대 실패: {ex.Message}");
            LogManager.Instance.ShowLog("친구 초대에 실패했습니다.");
        }
        finally
        {
            inviteFriendButton.interactable = true;
        }
    }
}
