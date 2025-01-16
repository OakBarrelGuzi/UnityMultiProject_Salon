using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Salon.Firebase;
using System;

public class SignUpPanel : Panel
{
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_InputField passwordConfirmInputField;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button signInButton;

    private void OnEnable()
    {
        Initialize();
    }

    public override void Initialize()
    {

        if (idInputField == null || passwordInputField == null || passwordConfirmInputField == null)
        {
            GetReferences();
        }

        if (idInputField != null) idInputField.text = "";
        if (passwordInputField != null) passwordInputField.text = "";
        if (passwordConfirmInputField != null) passwordConfirmInputField.text = "";

        if (signUpButton != null)
        {
            signUpButton.onClick.RemoveAllListeners();
            signUpButton.onClick.AddListener(OnSignUpButtonClick);
        }
        else
        {
            Debug.LogError("[SignUpPanel] SignUp 버튼이 null입니다");
        }

        if (signInButton != null)
        {
            signInButton.onClick.RemoveAllListeners();
            signInButton.onClick.AddListener(OnSignInButtonClick);
        }
        else
        {
            Debug.LogError("[SignUpPanel] SignIn 버튼이 null입니다");
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
            else if (inputField.name == "PasswordConfirm_Input")
                passwordConfirmInputField = inputField;
        }

        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            if (button.name == "SignUp_Button")
                signUpButton = button;
            else if (button.name == "SignIn_Button")
                signInButton = button;
        }

        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpButtonClick);
        if (signInButton != null)
            signInButton.onClick.AddListener(OnSignInButtonClick);
    }

    public async void OnSignUpButtonClick()
    {
        if (idInputField == null || passwordInputField == null || passwordConfirmInputField == null)
        {
            LogManager.Instance.ShowLog("SignUpPanel 이 초기화되지 않았습니다.");
            return;
        }
        if (idInputField.text == "" || passwordInputField.text == "" || passwordConfirmInputField.text == "")
        {
            LogManager.Instance.ShowLog("아이디, 비밀번호, 비밀번호 확인을 모두 입력해주세요.");
            return;
        }
        if (passwordInputField.text != passwordConfirmInputField.text)
        {
            LogManager.Instance.ShowLog("비밀번호와 비밀번호 확인이 일치하지 않습니다.");
            return;
        }
        bool result = await FirebaseManager.Instance.RegisterWithEmailAsync(idInputField.text, passwordInputField.text);
        try
        {
            if (result)
            {
                Close();
                UIManager.Instance.OpenPanel(PanelType.SignIn);
            }
            else
                LogManager.Instance.ShowLog("회원가입에 실패했습니다.");
        }
        catch (Exception ex)
        {
            LogManager.Instance.ShowLog($"예외 발생: {ex.Message}");
        }
    }

    public void OnSignInButtonClick()
    {
        Close();
        UIManager.Instance.OpenPanel(PanelType.SignIn);
    }

}