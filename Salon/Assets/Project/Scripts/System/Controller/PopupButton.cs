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
        Debug.Log($"[PopupButton] ���� ��ȣ�ۿ� Ÿ�� ����: {currentInteraction}");
    }

    private void HandleButtonClick()
    {
        Debug.Log($"[PopupButton] ��ư Ŭ���� - ��ȣ�ۿ� Ÿ��: {currentInteraction}");
        HandleInteraction(currentInteraction); // ���� ������ InteractionType ó��
    }

    private void HandleInteraction(InteractionType interactionType)
    {
        switch (interactionType)
        {
            case InteractionType.Shop:
                Debug.Log("���� ����");
                OpenShop();
                break;

            case InteractionType.ShellGame:
                Debug.Log("�� ���� ����");
                StartShellGame();
                break;

            case InteractionType.CardGame:
                StartCardGame();
                break;

            default:
                Debug.LogWarning("�� �� ���� ��ȣ�ۿ� Ÿ��");
                break;
        }
    }

    // �� InteractionType�� ���� ó�� �޼���
    private void OpenShop()
    {
        Debug.Log("[PopupButton] ������ �������ϴ�!");
        // ���� ���� ���� ����
    }
    private void StartShellGame()
    {
        Debug.Log("[PopupButton] �� ������ �����߽��ϴ�!");
        // �� ���� ���� ���� ����
    }
    private async void StartCardGame()
    {
        await GameRoomManager.Instance.JoinOrCreateRandomRoom(ChannelManager.Instance.CurrentChannel, 
            ChannelManager.Instance.currentUserDisplayName);
    }
}
