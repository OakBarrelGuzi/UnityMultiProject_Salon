using UnityEngine;
using System.Collections.Generic;
using Salon.Firebase;
using Salon.Firebase.Database;
public class LobbyPanel : Panel
{

    public Transform channelParent;
    private List<ChannelButton> channelButtons = new List<ChannelButton>();
    public ChannelButton channelButtonPrefab;

    public override void Initialize()
    {
        channelButtons.Clear();
        foreach (Transform child in channelParent)
        {
            Destroy(child.gameObject);
        }

        GetChannels();
    }

    private async void GetChannels()
    {
        Dictionary<string, ChannelData> channelData = await FirebaseManager.Instance.channelManager.WaitForChannelData();
        foreach (var channel in channelData)
        {
            ChannelButton button = Instantiate(channelButtonPrefab, channelParent);
            button.Initialize(channel.Key, channel.Value.UserCount);
            button.button.onClick.AddListener(() => EnterChannel(channel.Key));
            channelButtons.Add(button);
        }
    }

    public void OnRefreshButtonClick()
    {
        Initialize();
    }

    public async void EnterChannel(string channelName)
    {
        await FirebaseManager.Instance.channelManager.EnterChannel(channelName);
    }
}