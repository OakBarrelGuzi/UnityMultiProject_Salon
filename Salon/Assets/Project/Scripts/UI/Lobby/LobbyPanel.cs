using UnityEngine;
using Salon.Firebase;
using Salon.Controller;

public class LobbyPanel : Panel
{
    public ChatPopUp chatPopUp;

    private void OnEnable()
    {
        Initialize();
    }

    public override void Initialize()
    {
        chatPopUp.Initialize();
        FirebaseManager.Instance.ChannelManager.OnReceiveChat += HandleChat;
    }

    public override void Close()
    {
        FirebaseManager.Instance.ChannelManager.OnReceiveChat -= HandleChat;
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