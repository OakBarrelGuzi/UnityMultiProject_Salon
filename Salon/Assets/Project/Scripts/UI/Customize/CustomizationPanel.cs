using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Salon.Firebase;
using Salon.System;

public class CustomizationPanel : Panel
{
    public TMP_InputField nicknameInputField;
    public Button saveButton;

    private void Start()
    {
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        }

        // 현재 사용자의 닉네임을 입력 필드에 표시
        if (nicknameInputField != null)
        {
            nicknameInputField.text = FirebaseManager.Instance.CurrnetUserDisplayName;
        }
    }

    private async void OnSaveButtonClicked()
    {
        if (string.IsNullOrEmpty(nicknameInputField.text))
        {
            LogManager.Instance.ShowLog("닉네임을 입력해주세요.");
            return;
        }

        saveButton.interactable = false;

        bool success = await FirebaseManager.Instance.UpdateUsername(nicknameInputField.text);

        if (success)
        {
            LogManager.Instance.ShowLog("닉네임이 성공적으로 변경되었습니다.");
        }
        else
        {
            LogManager.Instance.ShowLog("닉네임 변경에 실패했습니다.");
            nicknameInputField.text = FirebaseManager.Instance.CurrnetUserDisplayName;
        }

        saveButton.interactable = true;
    }

    private void OnDestroy()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveListener(OnSaveButtonClicked);
        }
    }
}
