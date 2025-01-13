using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LogPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button copyButton;

    private string fullErrorMessage;
    private Action onClose;

    void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(HandleClose);

        if (copyButton != null)
            copyButton.onClick.AddListener(() => GUIUtility.systemCopyBuffer = fullErrorMessage);
    }

    public void ShowError(string message, string stackTrace, Action onCloseCallback = null)
    {
        if (errorText != null)
            errorText.text = message;

        fullErrorMessage = $"{message}\n\n스택 트레이스:\n{stackTrace}";
        onClose = onCloseCallback;
        gameObject.SetActive(true);
    }

    private void HandleClose()
    {
        gameObject.SetActive(false);
        onClose?.Invoke();
    }

    void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();

        if (copyButton != null)
            copyButton.onClick.RemoveAllListeners();
    }
}