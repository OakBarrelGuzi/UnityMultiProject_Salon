using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Salon.Firebase;
using Salon.Firebase.Database;
using System;

public class ChatPopUp : MonoBehaviour
{
    public ChatBox chatBoxPrefab;
    public Transform chatBoxParent;
    public TMP_InputField messageInputField;

    public void Initialize()
    {
        messageInputField.onEndEdit.AddListener((string text) => SendChat(text));
    }

    public async void SendChat(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        await ChatManager.Instance.SendChat(message, ChannelManager.Instance.CurrentChannel, FirebaseManager.Instance.GetCurrentDisplayName());
        messageInputField.text = "";
    }

    public void ReceiveChat(string sender, string message, Sprite emoji = null)
    {
        ChatBox chatBox = Instantiate(chatBoxPrefab, chatBoxParent);
        chatBox.SetChatBox(DisplayNameUtils.RemoveTag(sender), message, emoji);
    }

    public void ClearChat()
    {
        foreach (Transform child in chatBoxParent)
        {
            Destroy(child.gameObject);
        }
    }

}
