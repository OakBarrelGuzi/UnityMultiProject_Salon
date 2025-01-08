using UnityEngine;
using TMPro;

public class SignInPanel : Panel
{
    private TMP_InputField idInputField;
    private TMP_InputField passwordInputField;

    public override void Initialize()
    {
        TMP_InputField[] inputFields = GetComponentsInChildren<TMP_InputField>();
        foreach (TMP_InputField inputField in inputFields)
        {
            if (inputField.name == "Email_Input")
                idInputField = inputField;
            else if (inputField.name == "Password_Input")
                passwordInputField = inputField;
        }
    }
}