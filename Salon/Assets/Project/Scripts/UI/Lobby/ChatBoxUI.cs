using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Salon.Firebase.Database;
using Salon.Firebase;

public class ChatBox : MonoBehaviour
{
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI messageText;
    public Image emojiImage;

    public void SetChatBox(string sender, string message, Sprite emoji = null)
    {
        if (sender == FirebaseManager.Instance.GetCurrentDisplayName())
        {
            senderText.text = $"<color=#7BFF75>{DisplayNameUtils.RemoveTag(sender)}</color> : ";
            messageText.text = $"<color=#7BFF75>{message}</color>";
        }
        else
        {
            senderText.text = $"{DisplayNameUtils.RemoveTag(sender)} : ";
            messageText.text = $"{message}";
        }

        if (emoji != null)
        {
            emojiImage.sprite = emoji;
            emojiImage.gameObject.SetActive(true);
        }
        else
        {
            emojiImage.gameObject.SetActive(false);
        }
    }
}
