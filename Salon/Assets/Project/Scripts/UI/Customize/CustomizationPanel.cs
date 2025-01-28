using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Salon.Firebase;
using Salon.System;

public class CustomizationPanel : Panel
{
    public TMP_InputField nicknameInputField;
    public Button saveButton;
    public Button mainMenuButton;

    public override void Open()
    {
        base.Open();
        Initialize();
    }

    public override void Initialize()
    {
        base.Initialize();

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveButtonClicked);
        }

        // 현재 사용자의 닉네임을 입력 필드에 표시
        if (nicknameInputField != null)
        {
            nicknameInputField.text = DisplayNameUtils.ToDisplayFormat(FirebaseManager.Instance.CurrnetUserDisplayName);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
    }

    private async void OnSaveButtonClicked()
    {
        if (!CheckNameValid(nicknameInputField.text))
        {
            return;
        }

        saveButton.interactable = false;

        bool success = await FirebaseManager.Instance.UpdateUsername(nicknameInputField.text);

        if (success)
        {
            LogManager.Instance.ShowLog("닉네임이 성공적으로 변경되었습니다.");
            nicknameInputField.text = DisplayNameUtils.ToDisplayFormat(FirebaseManager.Instance.GetCurrentDisplayName());
        }
        else
        {
            LogManager.Instance.ShowLog("닉네임 변경에 실패했습니다.");
            nicknameInputField.text = DisplayNameUtils.ToDisplayFormat(FirebaseManager.Instance.CurrnetUserDisplayName);
        }

        saveButton.interactable = true;
    }

    public bool CheckNameValid(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            LogManager.Instance.ShowLog("닉네임을 입력해주세요.");
            return false;
        }

        if (name.Contains("_") || name.Contains(" ") || name.Contains("#"))
        {
            LogManager.Instance.ShowLog("닉네임에 특수문자는 사용할수 없습니다");
            return false;
        }

        return true;
    }

    public void OnMainMenuButtonClicked()
    {
        UIManager.Instance.CloseAllPanels();
        UIManager.Instance.OpenPanel(PanelType.MainDisplay);
        ScenesManager.Instance.ChanageScene("MainScene");
    }

    public override void Close()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
        }
        base.Close();
    }
}
