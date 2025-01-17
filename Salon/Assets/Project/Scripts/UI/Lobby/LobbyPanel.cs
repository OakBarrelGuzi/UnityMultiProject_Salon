using UnityEngine;
using Salon.Firebase;
using Salon.Controller;
using UnityEngine.UI;

public class LobbyPanel : Panel
{
    public ChatPopUp chatPopUp;
    public Button friendsButton;

    private void OnEnable()
    {
        Initialize();
    }

    public override void Initialize()
    {
        chatPopUp.Initialize();
        ChatManager.Instance.OnReceiveChat += HandleChat;
        friendsButton.onClick.AddListener(OnFriendsButtonClick);
    }

    private void OnFriendsButtonClick()
    {
        UIManager.Instance.OpenPanel(PanelType.Friends);
    }

    public override void Close()
    {
        ChatManager.Instance.OnReceiveChat -= HandleChat;
        chatPopUp.ClearChat();
        base.Close();
    }

    private void HandleChat(string sender, string message, Sprite emoji)
    {
        if (chatPopUp != null)
        {
            chatPopUp.ReceiveChat(sender, message, emoji);
        }
    }
}