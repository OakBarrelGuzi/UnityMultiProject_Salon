using UnityEngine;
using Salon.Firebase;
using Salon.Controller;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Salon.Firebase.Database;
using Firebase.Database;

public class LobbyPanel : Panel
{
    public ChatPopUp chatPopUp;
    public Button friendsButton;
    public Button inventoryButton;
    public Button animButton;
    public Button emojiButton;
    public TextMeshProUGUI goldText;
    public DatabaseReference goldRef;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override void Close()
    {
        base.Close();
    }

    public override void Initialize()
    {
        RemoveAllListeners();
        chatPopUp.Initialize();
        ChatManager.Instance.OnReceiveChat += HandleChat;
        SetupButtonListeners();
        UpdateGoldText();

        goldRef = FirebaseManager.Instance.DbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Gold");
        goldRef.ValueChanged += OnGoldChanged;
    }

    private void RemoveAllListeners()
    {
        friendsButton.onClick.RemoveAllListeners();
        inventoryButton.onClick.RemoveAllListeners();
        animButton.onClick.RemoveAllListeners();
        emojiButton.onClick.RemoveAllListeners();
        if (goldRef != null)
        {
            goldRef.ValueChanged -= OnGoldChanged;
        }
        ChatManager.Instance.OnReceiveChat -= HandleChat;
    }

    private void SetupButtonListeners()
    {
        friendsButton.onClick.AddListener(OnFriendsButtonClick);

        animButton.onClick.AddListener(() =>
        {
            var panel = UIManager.Instance.GetPanelByType(PanelType.Animation);
            bool isOpen = panel?.isOpen ?? false;
            if (isOpen)
                UIManager.Instance.ClosePanel(PanelType.Animation);
            else
            {
                if (UIManager.Instance.GetPanelByType(PanelType.Emoji) != null)
                    UIManager.Instance.ClosePanel(PanelType.Emoji);
                UIManager.Instance.OpenPanel(PanelType.Animation);
            }
        });

        emojiButton.onClick.AddListener(() =>
        {
            var panel = UIManager.Instance.GetPanelByType(PanelType.Emoji);
            bool isOpen = panel?.isOpen ?? false;
            if (isOpen)
                UIManager.Instance.ClosePanel(PanelType.Emoji);
            else
            {
                if (UIManager.Instance.GetPanelByType(PanelType.Animation) != null)
                    UIManager.Instance.ClosePanel(PanelType.Animation);
                UIManager.Instance.OpenPanel(PanelType.Emoji);
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

    private void OnGoldChanged(object sender, ValueChangedEventArgs e)
    {
        UpdateGoldText();
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

    private void HandleChat(string sender, string message, Sprite emoji)
    {
        if (chatPopUp != null)
        {
            chatPopUp.ReceiveChat(sender, message, emoji);
        }
    }
}