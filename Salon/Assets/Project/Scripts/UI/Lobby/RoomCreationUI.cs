using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Salon.Firebase;
using TMPro;
using System;

public class RoomCreationUI : Panel
{
    [Header("UI Elements")]
    public Button closeButton;
    public GameObject findOp;
    public GameObject matching;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override void Initialize()
    {
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(OnCloseClick);
        findOp.SetActive(false);
    }
    public void OnFind()
    {
        matching.SetActive(false);
        findOp.SetActive(true);
    }

    public void OnCloseClick()
    {
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }
}
