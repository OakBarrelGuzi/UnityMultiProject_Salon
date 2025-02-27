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

    public override void Initialize()
    {
        startButton.onClick.AddListener(StartButtonClick);
        rankingButton.onClick.AddListener(RankingButtonClick);
        closeButton.onClick.AddListener(Close);
    }

    private void StartButtonClick()
    {
        UIManager.Instance.ClosePanel(PanelType.DartGame);
        ScenesManager.Instance.ChanageScene("Jindarts");
    }
    private void RankingButtonClick()
    {
        UIManager.Instance.ClosePanel(PanelType.DartGame);
        UIManager.Instance.OpenPanel(PanelType.DartRanking);
    }

    public override void Close()
    {
        startButton.onClick.RemoveAllListeners();
        rankingButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }

}
