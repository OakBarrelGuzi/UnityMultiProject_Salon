using Salon.System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : Singleton<ScenesManager>
{
    [Header("SceneSet")]
    public string[] playScenes;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void ChanageScene(string nextScene)
    {
        SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
    }
}
