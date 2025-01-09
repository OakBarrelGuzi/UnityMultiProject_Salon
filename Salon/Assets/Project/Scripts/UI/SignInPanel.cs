using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Salon.Firebase;
using Firebase;
using System;

public class SignInPanel : Panel
{
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button signInButton;
    [SerializeField] private Button signUpButton;

    void OnEnable()
    {
        Initialize();
    }

    public override void Initialize()
    {
        if (idInputField == null || passwordInputField == null)
        {
            GetReferences();
        }
        else
        {
            if (idInputField != null)
                idInputField.text = "";
            if (passwordInputField != null)
                passwordInputField.text = "";
            if (signInButton != null)
                signInButton.onClick.AddListener(OnSignInButtonClick);
            if (signUpButton != null)
                signUpButton.onClick.AddListener(OnSignUpButtonClick);

        }
    }

    private void GetReferences()
    {
        TMP_InputField[] inputFields = GetComponentsInChildren<TMP_InputField>();
        foreach (TMP_InputField inputField in inputFields)
        {
            if (inputField.name == "Email_Input")
                idInputField = inputField;
            else if (inputField.name == "Password_Input")
                passwordInputField = inputField;
            else if (inputField.name == "SignUp_Button")
                signUpButton = inputField.GetComponent<Button>();
            else if (inputField.name == "SignIn_Button")
                signInButton = inputField.GetComponent<Button>();
        }
    }

    public async void OnSignInButtonClick()
    {
        ValidateInput();
        try
        {
            bool result = await FirebaseManager.Instance.SignInWithEmailAsync(idInputField.text, passwordInputField.text);
            if (result)
                UIManager.Instance.OpenPanel(PanelType.Lobby);
            else
                LogManager.Instance.ShowLog("로그인에 실패했습니다.");
        }
        catch (Exception ex)
        {
            LogManager.Instance.ShowLog($"예외 발생: {ex.Message}");
        }
    }

    private void ValidateInput()
    {
        if (idInputField == null || passwordInputField == null)
        {
            LogManager.Instance.ShowLog("SignInPanel 이 초기화되지 않았습니다.");
            return;
        }
        if (idInputField.text == "" || passwordInputField.text == "")
        {
            LogManager.Instance.ShowLog("아이디 또는 비밀번호를 입력해주세요.");
            return;
        }
    }

    public void OnSignUpButtonClick()
    {
        Close();
        UIManager.Instance.OpenPanel(PanelType.SignUp);
    }
}