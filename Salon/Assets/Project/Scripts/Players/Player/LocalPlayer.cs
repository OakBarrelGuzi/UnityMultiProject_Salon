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
        [SerializeField]
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
        }

        private void Update()
        {
            if (Time.time - lastPositionUpdateTime >= positionUpdateInterval)
            {
                UpdateAndSendPosition();
            }
        }

        private async void UpdateAndSendPosition()
        {
            try
            {
                var newPosition = UpdateNetworkPosition();
                Debug.Log($"[LocalPlayer] 새 위치 데이터 생성: Pos={newPosition.GetPosition()}, Dir={newPosition.GetDirection()}, IsUpdate={newPosition.IsPositionUpdate}");

                // 위치가 변경되었을 때만 전송
                if (HasPositionChanged(newPosition))
                {
                    Debug.Log("[LocalPlayer] 위치 변경 감지됨");
                    if (lastSentPosition != null)
                    {
                        Debug.Log($"[LocalPlayer] 이전 위치: Pos={lastSentPosition.GetPosition()}, Dir={lastSentPosition.GetDirection()}");
                    }

                    lastPositionUpdateTime = Time.time;
                    lastSentPosition = newPosition;

                    var channelManager = FirebaseManager.Instance.ChannelManager;
                    if (channelManager != null && !string.IsNullOrEmpty(channelManager.CurrentChannel))
                    {
                        Debug.Log($"[LocalPlayer] 현재 채널: {channelManager.CurrentChannel}");
                        var dbReference = FirebaseManager.Instance.DbReference;
                        if (dbReference != null)
                        {
                            var playerRef = dbReference.Child("Channels")
                                .Child(channelManager.CurrentChannel)
                                .Child("Players")
                                .Child(FirebaseManager.Instance.CurrentUserName);

                            // 현재 플레이어 데이터를 가져옴
                            var snapshot = await playerRef.GetValueAsync();
                            if (snapshot.Exists)
                            {
                                var playerData = JsonConvert.DeserializeObject<GamePlayerData>(snapshot.GetRawJsonValue());
                                playerData.Position = newPosition;  // 위치 업데이트

                                string jsonData = JsonConvert.SerializeObject(playerData);
                                Debug.Log($"[LocalPlayer] Firebase에 전송할 JSON 데이터: {jsonData}");
                                Debug.Log($"[LocalPlayer] 데이터 경로: Channels/{channelManager.CurrentChannel}/Players/{FirebaseManager.Instance.CurrentUserName}");

                                await playerRef.SetRawJsonValueAsync(jsonData);
                                Debug.Log("[LocalPlayer] Firebase에 위치 데이터 전송 완료");
                            }
                            else
                            {
                                Debug.LogError("[LocalPlayer] 플레이어 데이터를 찾을 수 없습니다");
                            }
                        }
                        else
                        {
                            Debug.LogError("[LocalPlayer] Firebase 데이터베이스 참조가 null입니다");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[LocalPlayer] 채널매니저 또는 현재 채널이 없음 - ChannelManager: {channelManager != null}, Channel: {channelManager?.CurrentChannel}");
                    }
                }
                else
                {
                    Debug.Log("[LocalPlayer] 위치 변경이 임계값보다 작아 전송하지 않음");
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

            float positionThreshold = 0.01f; // 1cm
            float directionThreshold = 0.1f;  // 약 5.7도

            Vector3? newPos = newPosition.GetPosition();
            Vector3? lastPos = lastSentPosition.GetPosition();

            if (!newPos.HasValue || !lastPos.HasValue)
            {
                Debug.Log("[LocalPlayer] 위치 값이 null");
                return true;
            }

            float positionDist = Vector3.Distance(newPos.Value, lastPos.Value);
            float directionDist = Vector3.Distance(newPosition.GetDirection(), lastSentPosition.GetDirection());

            bool positionChanged = positionDist > positionThreshold;
            bool directionChanged = directionDist > directionThreshold;

            Debug.Log($"[LocalPlayer] 위치 변경 체크 - 위치 거리: {positionDist:F3}, 방향 거리: {directionDist:F3}");
            Debug.Log($"[LocalPlayer] 변경 여부 - 위치: {positionChanged}, 방향: {directionChanged}");

            return positionChanged || directionChanged;
        }

        public NetworkPositionData UpdateNetworkPosition()
        {
            Vector3 direction = inputController != null ? inputController.CurrentVelocity.normalized : transform.forward;
            if (direction == Vector3.zero) direction = transform.forward;

            Vector3 currentPos = transform.position;
            Debug.Log($"[LocalPlayer] 현재 실제 위치: {currentPos}, 방향: {direction}");

            return new NetworkPositionData(currentPos, direction, true);
        }
    }
}