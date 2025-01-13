using UnityEngine;
using Salon.Controller;
using Salon.Firebase.Database;
using Salon.Firebase;
using System;
using Newtonsoft.Json;

namespace Salon.Character
{
    public class LocalPlayer : Player
    {
        private GamePlayerData cachedPlayerData;
        private float positionUpdateInterval = 0.1f;
        private float lastPositionUpdateTime;
        private InputController inputController;
        private NetworkPositionData lastSentPosition;

        public override void Initialize(string displayName)
        {
            base.Initialize(displayName);
            inputController = GetComponent<InputController>();
            if (inputController != null)
            {
                inputController.enabled = true;
                inputController.Initialize();
            }
            lastPositionUpdateTime = Time.time;
            lastSentPosition = new NetworkPositionData(transform.position, transform.forward, true);
            cachedPlayerData = new GamePlayerData(displayName);
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
                var newPosition = UpdateNetworkPosition();

                if (HasPositionChanged(newPosition))
                {
                    lastPositionUpdateTime = Time.time;
                    lastSentPosition = newPosition;

                    var channelManager = FirebaseManager.Instance.ChannelManager;
                    if (channelManager != null && !string.IsNullOrEmpty(channelManager.CurrentChannel))
                    {
                        var dbReference = FirebaseManager.Instance.DbReference;
                        if (dbReference != null)
                        {
                            var playerRef = dbReference.Child("Channels")
                                .Child(channelManager.CurrentChannel)
                                .Child("Players")
                                .Child(FirebaseManager.Instance.CurrentUserName)
                                .Child("Position");

                            string jsonData = JsonConvert.SerializeObject(newPosition);
                            Debug.Log($"[LocalPlayer] Firebase에 전송할 위치 데이터: {jsonData}");

                            await playerRef.SetRawJsonValueAsync(jsonData);
                            Debug.Log("[LocalPlayer] Firebase에 위치 데이터 전송 완료");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LocalPlayer] 위치 업데이트 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }

        private bool HasPositionChanged(NetworkPositionData newPosition)
        {
            if (lastSentPosition == null)
            {
                Debug.Log("[LocalPlayer] 첫 위치 업데이트");
                return true;
            }

            bool hasChanged = newPosition.HasSignificantChange(lastSentPosition);
            if (hasChanged)
            {
                Debug.Log($"[LocalPlayer] 위치 변경 감지 - 이전: ({lastSentPosition.PosX}, {lastSentPosition.PosZ}), " +
                    $"새 위치: ({newPosition.PosX}, {newPosition.PosZ})");
            }

            return hasChanged;
        }

        public NetworkPositionData UpdateNetworkPosition()
        {
            Vector3 velocity = inputController != null ? inputController.CurrentVelocity : Vector3.zero;
            Vector3 direction = velocity.normalized;
            if (direction == Vector3.zero) direction = transform.forward;

            Vector3 currentPos = transform.position;
            bool isMoving = velocity.magnitude > 0.01f;

            return new NetworkPositionData(currentPos, direction, isMoving);
        }
    }
}