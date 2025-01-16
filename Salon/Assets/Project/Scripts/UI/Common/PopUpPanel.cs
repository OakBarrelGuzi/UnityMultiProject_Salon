using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.Events;

namespace Salon.UI
{
    public class PopUpPanel : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button yesButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private CanvasGroup canvasGroup;

        public UnityEvent OnClose = new UnityEvent();
        private Action onAcceptAction;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void Initialize(Action acceptAction = null, string message = "")
        {
            onAcceptAction = acceptAction;
            SetMessage(message);
            SetupButtons();
            Show();
        }

        private void SetMessage(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                messageText.text = message;
            }
        }

        private void SetupButtons()
        {
            yesButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();

            yesButton.onClick.AddListener(OnAcceptButtonClicked);
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnAcceptButtonClicked()
        {
            onAcceptAction?.Invoke();
            Close();
        }

        private void OnCloseButtonClicked()
        {
            Close();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Close()
        {
            OnClose.Invoke();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            yesButton.onClick.RemoveAllListeners();
            closeButton.onClick.RemoveAllListeners();
        }
    }
}