using UnityEngine;
using Salon.Firebase;
using Salon.Controller;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Salon.Firebase.Database;

public class LobbyPanel : Panel
{
    public ChatPopUp chatPopUp;
    public Button friendsButton;
    public Button inventoryButton;
    public Button animButton;
    public Button emojiButton;
    public TextMeshProUGUI goldText;

    private void OnEnable()
    {
        Initialize();
    }

    public override void Initialize()
    {
        chatPopUp.Initialize();
        ChatManager.Instance.OnReceiveChat += HandleChat;
        friendsButton.onClick.AddListener(OnFriendsButtonClick);
        UpdateGoldText();
        animButton.onClick.AddListener(() =>
        {
            var panel = UIManager.Instance.GetPanelByType(PanelType.AnimActivated);
            bool isOpen = panel?.isOpen ?? false;
            if (isOpen)
                UIManager.Instance.ClosePanel(PanelType.AnimActivated);
            else
            {
                if (UIManager.Instance.GetPanelByType(PanelType.EmojiActivated) != null)
                    UIManager.Instance.ClosePanel(PanelType.EmojiActivated);
                UIManager.Instance.OpenPanel(PanelType.AnimActivated);
            }
        });
        emojiButton.onClick.AddListener(() =>
        {
            var panel = UIManager.Instance.GetPanelByType(PanelType.EmojiActivated);
            bool isOpen = panel?.isOpen ?? false;
            if (isOpen)
                UIManager.Instance.ClosePanel(PanelType.EmojiActivated);
            else
            {
                if (UIManager.Instance.GetPanelByType(PanelType.AnimActivated) != null)
                    UIManager.Instance.ClosePanel(PanelType.AnimActivated);
                UIManager.Instance.OpenPanel(PanelType.EmojiActivated);
            }
        });
        inventoryButton.onClick.AddListener(() =>
        {
            bool isOpen = UIManager.Instance.GetPanelByType(PanelType.Inventory)?.isOpen ?? false;
            if (isOpen)
                UIManager.Instance.ClosePanel(PanelType.Inventory);
            else
            {
                UIManager.Instance.OpenPanel(PanelType.Inventory);
            }
        });
    }

    public async void UpdateGoldText()
    {
        var currentUserRef = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID);
        var currentUserSnapshot = await currentUserRef.GetValueAsync();
        var currentUser = JsonConvert.DeserializeObject<UserData>(currentUserSnapshot.GetRawJsonValue());
        goldText.text = currentUser.Gold.ToString();
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