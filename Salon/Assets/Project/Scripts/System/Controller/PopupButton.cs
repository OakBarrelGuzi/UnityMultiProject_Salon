using Salon.Firebase;
using Salon.Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupButton : MonoBehaviour
{
    [SerializeField] private Button popupButton;
    private InteractionType currentInteraction = InteractionType.None;

    public Action<InteractionType> OnInteractionTriggered;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (popupButton == null)
        {
            popupButton = GetComponent<Button>();
        }

        popupButton.onClick.AddListener(() => HandleButtonClick());
    }

    public void SetInteraction(InteractionType interactionType)
    {
        currentInteraction = interactionType;
        Debug.Log($"[PopupButton] 현재 상호작용 타입 설정: {currentInteraction}");
    }

    private void HandleButtonClick()
    {
        Debug.Log($"[PopupButton] 버튼 클릭됨 - 상호작용 타입: {currentInteraction}");
        HandleInteraction(currentInteraction); // 현재 설정된 InteractionType 처리
    }

    private void HandleInteraction(InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Shop:
                Debug.Log("상점 열기");
                OpenShop();
                break;

            case InteractionType.ShellGame:
                Debug.Log("쉘 게임 시작");
                StartShellGame();
                break;

            case InteractionType.CardGame:
                StartCardGame();
                break;

            default:
                Debug.LogWarning("알 수 없는 상호작용 타입");
                break;
        }
    }

    // 각 InteractionType별 로직 처리 메서드
    private void OpenShop()
    {
        Debug.Log("[PopupButton] 상점을 열었습니다!");

        UIManager.Instance.OpenPanel(PanelType.Shop);
    }
    private void StartShellGame()
    {
        Debug.Log("[PopupButton] 쉘 게임을 시작했습니다!");
        // 쉘 게임 시작 로직 구현
    }
    private async void StartCardGame()
    {
        await GameRoomManager.Instance.JoinOrCreateRandomRoom(ChannelManager.Instance.CurrentChannel,
            ChannelManager.Instance.currentUserDisplayName);
    }
}
