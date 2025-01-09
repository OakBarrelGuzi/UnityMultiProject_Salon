using UnityEngine;
using System.Collections.Generic;
using Salon.Firebase;
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

    private void GetChannels()
    {
        //foreach (var channel in FirebaseManager.Instance.channelManager.channelData)
        //{
        //    ChannelButton button = Instantiate(channelButtonPrefab, channelParent);
        //    button.Initialize(channel.Key, channel.Value.playerCount);
        //    button.button.onClick.AddListener(() => enterChannel(channel.Key));
        //    channelButtons.Add(button);
        //}
    }

    public void OnRefreshButtonClick()
    {
        Initialize();
    }

    public async void enterChannel(string channelName)
    {
        //await FirebaseManager.Instance.channelManager.EnterChannel(channelName);
    }
}