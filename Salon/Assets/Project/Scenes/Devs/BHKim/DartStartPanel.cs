using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DartStartPanel : Panel
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button rankingButton;
    [SerializeField] private Button closeButton;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    private void Initialize()
    {
        startButton.onClick.AddListener(() => ScenesManager.Instance.ChanageScene("Jindarts"));
        //rankingButton.onClick.AddListener();
        closeButton.onClick.AddListener(Close);
    }
    public override void Close()
    {
        startButton.onClick.RemoveAllListeners();
        //rankingButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }
}
