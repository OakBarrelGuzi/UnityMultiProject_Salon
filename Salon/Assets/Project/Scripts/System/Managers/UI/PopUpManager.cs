using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Salon.System;

namespace Salon.UI
{
    public class PopUpManager : Singleton<PopUpManager>
    {
        [SerializeField] private PopUpPanel popUpPanelPrefab;
        [SerializeField] private Transform popUpParent;

        private PopUpPanel currentPopUp;
        private Queue<PopUpData> popUpQueue = new Queue<PopUpData>();
        private bool isProcessing;

        protected override void Awake()
        {
            base.Awake();
            if (popUpParent == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    var canvasObject = new GameObject("PopUpCanvas");
                    canvas = canvasObject.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;
                    canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                    canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                popUpPanelPrefab = Resources.Load<PopUpPanel>("UI/PopUpPanel");

                var parentObject = new GameObject("PopUpParent");
                popUpParent = parentObject.transform;
                popUpParent.SetParent(canvas.transform, false);
                var rectTransform = parentObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
            }
        }

        public void ShowPopUp(string message, Action onAccept = null, Action onDecline = null, bool useQueue = true)
        {
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogWarning("[PopUpManager] 메시지가 비어있습니다.");
                return;
            }

            var popUpData = new PopUpData(message, onAccept, onDecline);

            if (useQueue)
            {
                popUpQueue.Enqueue(popUpData);
                if (!isProcessing)
                {
                    ProcessNextPopUp();
                }
            }
            else
            {
                ShowPopUpImmediate(popUpData);
            }
        }

        private void ShowPopUpImmediate(PopUpData popUpData)
        {
            if (currentPopUp != null)
            {
                currentPopUp.Close();
            }

            if (popUpPanelPrefab == null)
            {
                Debug.LogError("[PopUpManager] PopUpPanel 프리팹이 설정되지 않았습니다.");
                return;
            }

            currentPopUp = Instantiate(popUpPanelPrefab, popUpParent);
            if (currentPopUp != null)
            {
                currentPopUp.Initialize(
                    () =>
                    {
                        popUpData.OnAccept?.Invoke();
                        OnPopUpClosed();
                    },
                    () =>
                    {
                        popUpData.OnDecline?.Invoke();
                        OnPopUpClosed();
                    },
                    popUpData.Message
                );

                currentPopUp.OnClose.AddListener(() =>
                {
                    OnPopUpClosed();
                });
            }
            else
            {
                Debug.LogError("[PopUpManager] PopUpPanel을 생성할 수 없습니다.");
            }
        }

        private void ProcessNextPopUp()
        {
            if (popUpQueue.Count == 0)
            {
                isProcessing = false;
                return;
            }

            isProcessing = true;
            var nextPopUp = popUpQueue.Dequeue();
            ShowPopUpImmediate(nextPopUp);
        }

        private void OnPopUpClosed()
        {
            if (currentPopUp != null)
            {
                Destroy(currentPopUp.gameObject);
                currentPopUp = null;
            }
            ProcessNextPopUp();
        }

        public void ClearQueue()
        {
            popUpQueue.Clear();
            isProcessing = false;
            if (currentPopUp != null)
            {
                Destroy(currentPopUp.gameObject);
                currentPopUp = null;
            }
        }

        private void OnDisable()
        {
            ClearQueue();
        }
    }

    public class PopUpData
    {
        public string Message { get; }
        public Action OnAccept { get; }
        public Action OnDecline { get; }

        public PopUpData(string message, Action onAccept = null, Action onDecline = null)
        {
            Message = message;
            OnAccept = onAccept;
            OnDecline = onDecline;
        }
    }
}