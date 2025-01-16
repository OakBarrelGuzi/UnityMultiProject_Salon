using UnityEngine;
using Salon.Controller;
using Salon.Firebase.Database;
using Salon.Firebase;
using System;
using Firebase.Database;

namespace Salon.Character
{
    public class LocalPlayer : Player
    {
        private DatabaseReference posRef;
        private float positionUpdateInterval = 0.5f;
        private float lastPositionUpdateTime;
        private InputController inputController;
        private string lastSentPositionData;

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
            lastSentPositionData = NetworkPositionCompressor.CompressVector3(transform.position, transform.forward, true);
            posRef = RoomManager.Instance.CurrentChannelPlayersRef.Child(displayName).Child("Position");
            CameraController cc = Camera.main.GetComponent<CameraController>();
            cc.SetTarget(transform);
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
    }
}