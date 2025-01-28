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
    private bool isGettingChannels = false;

    void OnEnable() => Initialize();

    public override void Initialize()
    {
        ClearChannelButtons();
        SetupButtons();
        GetChannels();
    }

    private void ClearChannelButtons()
    {
        if (channelButtons == null) return;

        foreach (ChannelButton button in channelButtons)
        {
            if (button != null)
            {
                if (button.button != null)
                    button.button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
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
        if (isGettingChannels)
        {
            Debug.Log("이미 채널 정보를 가져오는 중입니다.");
            return;
        }

        try
        {
            isGettingChannels = true;

            if (channelButtonPrefab == null)
            {
                Debug.LogError("channelButtonPrefab이 할당되지 않았습니다!");
                return;
            }

            Debug.Log("채널 데이터 요청 시작...");
            var channelData = await ChannelManager.Instance.WaitForChannelData();

            if (channelData == null)
            {
                Debug.LogError("채널 데이터를 가져오지 못했습니다!");
                return;
            }

            Debug.Log($"채널 데이터 수신 완료. 채널 수: {channelData.Count}");
            await CreateChannelButtons(channelData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"채널 정보 가져오기 실패: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            isGettingChannels = false;
        }
    }

    private async Task CreateChannelButtons(Dictionary<string, ChannelData> channelData)
    {
        try
        {
            foreach (var channel in channelData)
            {
                if (channelParent == null)
                {
                    Debug.LogError("channelParent가 null입니다!");
                    return;
                }

                var button = Instantiate(channelButtonPrefab, channelParent);
                if (button == null)
                {
                    Debug.LogError($"채널 버튼 생성 실패: {channel.Key}");
                    continue;
                }

                int userCount = await ChannelManager.Instance.GetChannelUserCount(channel.Key);
                Debug.Log($"채널 {channel.Key}의 현재 사용자 수: {userCount}");

                button.Initialize(channel.Key, userCount);
                button.button.onClick.AddListener(() => OnChannelButtonClick(channel.Key));
                channelButtons.Add(button);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"채널 버튼 생성 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
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
            UIManager.Instance.CloseAllPanels();
            Debug.Log(UIManager.Instance);
            UIManager.Instance.OpenPanel(PanelType.Lobby);
            UIManager.Instance.OpenPanel(PanelType.Loading);
            Debug.Log("ChannelManager : " + ChannelManager.Instance);
            await ChannelManager.Instance.JoinChannel(channelName);
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