using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBox : MonoBehaviour
{
    public TextMeshProUGUI senderText;
    public TextMeshProUGUI messageText;
    public Image emojiImage;

    public void SetChatBox(string sender, string message, Sprite emoji = null)
    {
        senderText.text = sender;
        messageText.text = message;
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
