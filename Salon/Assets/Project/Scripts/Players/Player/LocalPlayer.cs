using UnityEngine;
using Salon.Controller;
using Salon.Firebase.Database;
using Salon.Firebase;
using System;
using Newtonsoft.Json;
using Firebase.Database;

namespace Salon.Character
{
    public class LocalPlayer : Player
    {
        private DatabaseReference posRef;
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
            posRef = RoomManager.Instance.CurrentChannelPlayersRef.Child(displayName).Child("Position");
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

                    string jsonData = JsonConvert.SerializeObject(newPosition);
                    Debug.Log($"[LocalPlayer] Firebase에 전송할 위치 데이터: {jsonData}");
                    await posRef.SetRawJsonValueAsync(jsonData);
                    Debug.Log("[LocalPlayer] Firebase에 위치 데이터 전송 완료");

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