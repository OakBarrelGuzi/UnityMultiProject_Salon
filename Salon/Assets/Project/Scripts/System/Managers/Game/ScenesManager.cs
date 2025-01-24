using Salon.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Salon.Firebase;
using Salon.Character;

public class ScenesManager : Singleton<ScenesManager>
{
    [Header("SceneSet")]
    public string[] playScenes;

    protected override void Awake()
    {
        base.Awake();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ChanageScene(string nextScene)
    {
        // 로비씬으로 돌아가기 전에 플레이어 위치 저장
        if (nextScene != "LobbyScene" && GameManager.Instance != null && GameManager.Instance.player != null)
        {
            RoomManager.Instance.savedPlayerPosition = GameManager.Instance.player.transform.position;
            Debug.Log($"[ScenesManager] 플레이어 위치 저장: {RoomManager.Instance.savedPlayerPosition}");
        }

        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
        UIManager.Instance.OpenPanel(PanelType.Loading);
    }

    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyScene")
        {
            // 씬 로드 완료 후 약간의 대기 시간을 줍니다
            await System.Threading.Tasks.Task.Delay(500);

            UIManager.Instance.OpenPanel(PanelType.Lobby);
            await RoomManager.Instance.JoinChannel(ChannelManager.Instance.CurrentChannel);
        }
        else
        {

            RoomManager.Instance.DestroyAllPlayers();
            await RoomManager.Instance.UnsubscribeFromChannel();

        }
    }
}
