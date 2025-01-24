using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryExit : MonoBehaviour
{
    public Button ExitYesButton;
    public Button ExitNoButton;

    void Start()
    {
        ExitYesButton.onClick.AddListener(() => ScenesManager.Instance.ChanageScene("LobbyScene"));

        ExitNoButton.onClick.AddListener(() => gameObject.SetActive(false));
    }
}
