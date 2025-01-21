using Salon.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Salon.Firebase;

public class ScenesManager : Singleton<ScenesManager>
{
    [Header("SceneSet")]
    public string[] playScenes;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void ChanageScene(string nextScene)
    {
        SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
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
            await RoomManager.Instance.UnsubscribeFromChannel();
        }
    }
}
