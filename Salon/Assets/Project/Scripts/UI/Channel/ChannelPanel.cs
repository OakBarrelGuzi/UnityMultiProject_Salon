using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Salon.Firebase;
using Salon.Firebase.Database;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public class ChannelPanel : Panel
{
    [Header("UI References")]
    public Transform channelParent;
    public ChannelButton channelButtonPrefab;
    public Button refreshButton;
    public Button closeButton;

    private List<ChannelButton> channelButtons = new List<ChannelButton>();
    private bool isProcessing = false;

    void OnEnable() => Initialize();

    public override void Initialize()
    {
        ClearChannelButtons();
        SetupButtons();
        GetChannels();
    }

    private void ClearChannelButtons()
    {
        foreach (ChannelButton button in channelButtons)
        {
            button.button.onClick.RemoveAllListeners();
            Destroy(button.gameObject);
        }
        channelButtons.Clear();
    }

    private void SetupButtons()
    {
        if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshButtonClick);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClick);
    }

    private async void GetChannels()
    {
        if (channelButtonPrefab == null)
        {
            Debug.LogError("channelButtonPrefab이 할당되지 않았습니다!");
            return;
        }

        var channelData = await ChannelManager.Instance.WaitForChannelData();
        if (channelData == null)
        {
            Debug.LogError("채널 데이터를 가져오지 못했습니다!");
            return;
        }

        await CreateChannelButtons(channelData);
    }

    private async Task CreateChannelButtons(Dictionary<string, ChannelData> channelData)
    {
        foreach (var channel in channelData)
        {
            var button = Instantiate(channelButtonPrefab, channelParent);
            button.Initialize(channel.Key, await ChannelManager.Instance.GetChannelUserCount(channel.Key));
            button.button.onClick.AddListener(() => OnChannelButtonClick(channel.Key));
            channelButtons.Add(button);
        }
    }

    private void OnChannelButtonClick(string channelName)
    {
        if (!isProcessing) StartChannelEnter(channelName);
    }

    private async void StartChannelEnter(string channelName)
    {
        if (isProcessing)
        {
            Debug.Log("이미 처리 중입니다.");
            return;
        }

        try
        {
            isProcessing = true;
            SetButtonsInteractable(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
            Debug.Log(UIManager.Instance);
            UIManager.Instance.OpenPanel(PanelType.Lobby);
            Debug.Log("ChannelManager : " + ChannelManager.Instance);
            await ChannelManager.Instance.JoinChannel(channelName);
            Close();
        }
        catch (Exception ex)
        {
            Debug.LogError($"채널 입장 실패: {ex.Message}");
            if (ex.Message.Contains("이미 방에 존재"))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
            }
        }
        finally
        {
            isProcessing = false;
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var button in channelButtons)
        {
            if (button.button != null) button.button.interactable = interactable;
        }

        if (refreshButton != null) refreshButton.interactable = interactable;
        if (closeButton != null) closeButton.interactable = interactable;
    }

    public void OnRefreshButtonClick()
    {
        ClearChannelButtons();
        GetChannels();
    }
    public void OnCloseButtonClick() => Close();
}