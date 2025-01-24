using Newtonsoft.Json;
using Salon.Firebase.Database;
using Salon.Firebase;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Firebase.Database;

public class CardPanel : MonoBehaviour
{
    public Button option_Button;
    public TextMeshProUGUI localPlayerScore;
    public TextMeshProUGUI remotePlayerScore;
    public TextMeshProUGUI localPlayerName;
    public TextMeshProUGUI remotePlayerName;
    public Slider localPlayerTime;
    public Slider remotePlayerTime;
    public MemoryExit MemoryExit;
    private DatabaseReference roomRef;
    private string currentPlayerId;

    private void Awake()
    {
        roomRef = GameRoomManager.Instance.roomRef;
        currentPlayerId = GameRoomManager.Instance.currentPlayerId;
        option_Button.onClick.AddListener(() => MemoryExit.gameObject.SetActive(true));
    }
    void OnEnable()
    {
        MemoryExit.gameObject.SetActive(false);
    }
    private async void Start()
    {
        await LoadPlayerData();
    }
    private async Task LoadPlayerData()
    {
        try
        {
            // ���� �÷��̾� ������ ��������
            var localPlayerRef = roomRef.Child("Players").Child(GameRoomManager.Instance.currentPlayerId);
            var localSnapshot = await localPlayerRef.GetValueAsync();

            if (localSnapshot.Exists)
            {
                var localData = JsonConvert.DeserializeObject<PlayerData>(localSnapshot.GetRawJsonValue());
                localPlayerName.text = localData.DisplayName;
                localPlayerScore.text = localData.Score.ToString();
            }

            var playersSnapshot = await roomRef.Child("Players").GetValueAsync();
            foreach (var player in playersSnapshot.Children)
            {
                if (player.Key != GameRoomManager.Instance.currentPlayerId)
                {
                    var remoteData = JsonConvert.DeserializeObject<PlayerData>(player.GetRawJsonValue());
                    remotePlayerName.text = remoteData.DisplayName;
                    remotePlayerScore.text = remoteData.Score.ToString();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"�÷��̾� ������ �ε� �� ���� �߻�: {ex.Message}");
        }
    }

}
