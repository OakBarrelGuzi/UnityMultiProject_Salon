using UnityEngine;
using Firebase.Database;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Salon.Firebase.Database;
using Salon.System;

namespace Salon.Firebase
{
    public class ChatManager : Singleton<ChatManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference channelsRef;
        private DatabaseReference currentChannelRef;
        private DatabaseReference currentChannelMessagesRef;
        private Query currentMessagesQuery;
        public Action<string, string, Sprite> OnReceiveChat;
        private string currentChannel;
        private long lastMessageTimestamp;

        void Start()
        {
            _ = Initialize();
        }

        public async Task Initialize()
        {
            dbReference = await GetDbReference();
            channelsRef = dbReference.Child("Channels");
            lastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Debug.Log("[ChatManager] 초기화 완료");
        }

        private async Task<DatabaseReference> GetDbReference()
        {
            int maxRetries = 5;
            int currentRetry = 0;
            int delayMs = 1000;

            while (currentRetry < maxRetries)
            {
                if (FirebaseManager.Instance.DbReference != null)
                {
                    return FirebaseManager.Instance.DbReference;
                }

                Debug.Log($"[ChatManager] Firebase 데이터베이스 참조 대기 중... (시도 {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("[ChatManager] Firebase 데이터베이스 참조를 가져올 수 없습니다.");
        }

        private void UpdateChannelReferences(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return;

            currentChannelRef = channelsRef.Child(channelName).Child("CommonChannelData");
            currentChannelMessagesRef = currentChannelRef.Child("Messages");
            currentChannel = channelName;
            Debug.Log($"[ChatManager] 채널 레퍼런스 업데이트 완료: {channelName}");
        }

        public async Task SendChat(string message, string channelName, string senderId)
        {
            if (string.IsNullOrEmpty(channelName)) return;

            try
            {
                if (currentChannel != channelName)
                {
                    UpdateChannelReferences(channelName);
                }

                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var messageData = new MessageData(
                    senderId,
                    message,
                    timestamp
                );

                string messageKey = $"{senderId}_{timestamp}";
                await currentChannelMessagesRef.Child(messageKey)
                    .SetRawJsonValueAsync(JsonConvert.SerializeObject(messageData));

                lastMessageTimestamp = timestamp;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 전송 실패: {ex.Message}");
            }
        }

        public async Task StartListeningToMessages(string channelName)
        {
            try
            {
                StopListeningToMessages();
                UpdateChannelReferences(channelName);

                currentMessagesQuery = currentChannelMessagesRef
                    .OrderByChild("Timestamp")
                    .StartAt(lastMessageTimestamp - 1);

                currentMessagesQuery.ChildAdded += HandleMessageReceived;
                Debug.Log($"[ChatManager] 채널 {channelName}의 메시지 구독 시작 (Timestamp: {lastMessageTimestamp})");

                // 시스템 메시지 전송
                await SendChat($"{FirebaseManager.Instance.GetCurrentDisplayName()}님이 입장하셨습니다.", channelName, "System");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 구독 시작 실패: {ex.Message}");
                throw;
            }
        }

        private async void LoadExistingMessages()
        {
            try
            {
                var snapshot = await currentChannelMessagesRef
                    .OrderByChild("Timestamp")
                    .LimitToLast(50)
                    .GetValueAsync();

                if (snapshot.Exists)
                {
                    foreach (var messageSnapshot in snapshot.Children)
                    {
                        var messageData = JsonConvert.DeserializeObject<MessageData>(messageSnapshot.GetRawJsonValue());
                        if (messageData.Timestamp > lastMessageTimestamp)
                        {
                            lastMessageTimestamp = messageData.Timestamp;
                        }
                        OnReceiveChat?.Invoke(messageData.SenderId, messageData.Content, null);
                    }
                    Debug.Log($"[ChatManager] 기존 메시지 로드 완료 (마지막 타임스탬프: {lastMessageTimestamp})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 기존 메시지 로드 실패: {ex.Message}");
            }
        }

        public void StopListeningToMessages()
        {
            try
            {
                if (currentMessagesQuery != null)
                {
                    currentMessagesQuery.ChildAdded -= HandleMessageReceived;
                    currentMessagesQuery = null;
                    Debug.Log("[ChatManager] 메시지 구독 해제 완료");
                }

                currentChannelMessagesRef = null;
                currentChannelRef = null;
                currentChannel = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 구독 해제 중 오류 발생: {ex.Message}");
            }
        }

        private void HandleMessageReceived(object sender, ChildChangedEventArgs args)
        {
            if (!args.Snapshot.Exists) return;

            try
            {
                var messageData = JsonConvert.DeserializeObject<MessageData>(args.Snapshot.GetRawJsonValue());

                if (messageData.Timestamp <= lastMessageTimestamp)
                {
                    return;
                }

                lastMessageTimestamp = messageData.Timestamp;
                Debug.Log($"[ChatManager] 새 메시지 수신: Sender={messageData.SenderId}, Content={messageData.Content}, Timestamp={messageData.Timestamp}");
                OnReceiveChat?.Invoke(messageData.SenderId, messageData.Content, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 처리 중 오류 발생: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            StopListeningToMessages();
        }
    }
}