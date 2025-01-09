using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChannelButton : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI channelName;
    public TextMeshProUGUI channelPlayerCount;

    public void Initialize(string channelName, int playerCount)
    {
        this.channelName.text = channelName;
        this.channelPlayerCount.text = $"{playerCount} / 10";
    }
}