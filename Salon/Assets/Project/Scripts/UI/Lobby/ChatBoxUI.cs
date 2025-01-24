using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Salon.Firebase.Database;
using Salon.Firebase;

public class ChatBox : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public void SetChatBox(string sender, string message, Sprite emoji = null)
    {
        messageText.wordWrappingRatios = 0.0f;
        if (sender == FirebaseManager.Instance.GetCurrentDisplayName())
        {
            messageText.text = $"<color=#7BFF75>{DisplayNameUtils.RemoveTag(sender)}</color> : <color=#7BFF75>{message}</color>";
        }
        else if (sender == "System")
        {
            messageText.text = $"<align=center>{message}</align>";
        }
        else
        {
            messageText.text = $"{DisplayNameUtils.RemoveTag(sender)} : {message}";
        }
    }
}
