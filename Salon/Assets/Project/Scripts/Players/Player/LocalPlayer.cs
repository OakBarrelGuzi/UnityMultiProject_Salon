using UnityEngine;
using Salon.Controller;
using Salon.Firebase.Database;
using Salon.Firebase;
using System;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;

namespace Salon.Character
{
    public class LocalPlayer : Player
    {
        private DatabaseReference posRef;
        private DatabaseReference AnimRef;
        private DatabaseReference EmojiRef;
        private float positionUpdateInterval = 0.5f;
        private float lastPositionUpdateTime;
        private InputController inputController;
        private string lastSentPositionData;
        public AnimController animController;

        public override void Initialize(string displayName)
        {
            print("LocalPlayer Initialize");
            base.Initialize(displayName);
            inputController = GetComponent<InputController>();
            if (inputController != null)
            {
                inputController.enabled = true;
                inputController.Initialize();
            }
            lastPositionUpdateTime = Time.time;
            lastSentPositionData = NetworkPositionCompressor.CompressVector3(transform.position, transform.forward, true);
            posRef = RoomManager.Instance.CurrentChannelPlayersRef.Child(displayName).Child("Position");
            AnimRef = RoomManager.Instance.CurrentChannelPlayersRef.Child(displayName).Child("Animation");
            EmojiRef = RoomManager.Instance.CurrentChannelPlayersRef.Child(displayName).Child("Emoji");

            if (animController != null)
            {
                animController.OnAnimationStateChanged += HandleAnimationStateChanged;
                animController.OnEmojiChanged += HandleEmojiChanged;
            }

            StartCoroutine(SetupCamera());
        }

        private IEnumerator SetupCamera()
        {
            int maxAttempts = 10;
            int currentAttempt = 0;

            while (currentAttempt < maxAttempts)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    CameraController cc = mainCam.GetComponent<CameraController>();
                    if (cc == null)
                    {
                        print("메인 카메라에 CameraController가 없어서 추가합니다.");
                        cc = mainCam.gameObject.AddComponent<CameraController>();
                    }

                    print($"카메라 컨트롤러 설정 완료: {mainCam.gameObject.name}");
                    cc.SetTarget(this.gameObject.transform);
                    yield break;
                }
                else
                {
                    print("메인 카메라를 찾을 수 없습니다. 재시도 중...");
                }

                currentAttempt++;
                yield return new WaitForSeconds(0.2f);
            }

            Debug.LogError("카메라 설정 실패: 메인 카메라를 찾을 수 없습니다.");
        }

        public async void OnAnim()
        {
            await AnimRef.SetValueAsync("1");
        }

        private void Update()
        {
            if (!isTesting)
            {
                if (Time.time - lastPositionUpdateTime >= positionUpdateInterval)
                {
                    UpdateAndSendPosition();
                }
            }
        }

        private async void UpdateAndSendPosition()
        {
            try
            {
                Vector3 velocity = inputController != null ? inputController.CurrentVelocity : Vector3.zero;
                Vector3 direction = velocity.normalized;
                if (direction == Vector3.zero) direction = transform.forward;

                bool isMoving = velocity.magnitude > 0.01f;
                string newPositionData = NetworkPositionCompressor.CompressVector3(transform.position, direction, isMoving);

                if (HasPositionChanged(newPositionData))
                {
                    lastPositionUpdateTime = Time.time;
                    lastSentPositionData = newPositionData;

                    Debug.Log($"[LocalPlayer] Firebase에 전송할 압축 데이터: {newPositionData}");
                    await posRef.SetValueAsync(newPositionData);
                    Debug.Log("[LocalPlayer] Firebase에 위치 데이터 전송 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayer] 위치 업데이트 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }

        private bool HasPositionChanged(string newPositionData)
        {
            if (string.IsNullOrEmpty(lastSentPositionData))
            {
                Debug.Log("[LocalPlayer] 첫 위치 업데이트");
                return true;
            }

            bool hasChanged = newPositionData != lastSentPositionData;
            if (hasChanged)
            {
                (Vector3 oldPos, Vector3 _, bool _) = NetworkPositionCompressor.DecompressToVectors(lastSentPositionData);
                (Vector3 newPos, Vector3 _, bool _) = NetworkPositionCompressor.DecompressToVectors(newPositionData);
                Debug.Log($"[LocalPlayer] 위치 변경 감지 - 이전: ({oldPos.x}, {oldPos.z}), 새 위치: ({newPos.x}, {newPos.z})");
            }

            return hasChanged;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Interactive"))
            {
                var interactionComponent = other.GetComponent<InteractionComponent>();
                if (interactionComponent != null)
                {
                    inputController.popupButton.SetInteraction(interactionComponent.InteractionType);

                    if (interactionComponent.InteractionType == InteractionType.DartGame)
                        UIManager.Instance.OpenPanel(PanelType.DartGame);
                    else
                        inputController.popupButton.gameObject.SetActive(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Interactive"))
            {
                inputController.popupButton.SetInteraction(InteractionType.None);
                if (other.GetComponent<InteractionComponent>().InteractionType == InteractionType.DartGame)
                    UIManager.Instance.ClosePanel(PanelType.DartGame);
                else
                    inputController.popupButton.gameObject.SetActive(false);
            }
        }

        private async void HandleEmojiChanged(string emojiName)
        {
            try
            {
                var emojiData = new Dictionary<string, object>
                {
                    { "name", emojiName },
                    { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                };
                await EmojiRef.SetValueAsync(emojiData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayer] 이모지 업데이트 실패: {ex.Message}");
            }
        }

        private async void HandleAnimationStateChanged(bool isPlaying)
        {
            if (inputController != null)
            {
                inputController.enabled = !isPlaying;
            }

            try
            {
                if (isPlaying && animController != null)
                {
                    var animData = new Dictionary<string, object>
                    {
                        { "name", animController.CurrentAnimationName },
                        { "timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
                    };
                    await AnimRef.SetValueAsync(animData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayer] 애니메이션 업데이트 실패: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (animController != null)
            {
                animController.OnAnimationStateChanged -= HandleAnimationStateChanged;
                animController.OnEmojiChanged -= HandleEmojiChanged;
            }
        }
    }
}