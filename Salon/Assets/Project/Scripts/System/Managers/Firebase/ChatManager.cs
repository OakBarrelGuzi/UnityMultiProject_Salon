using UnityEngine;
using Firebase.Database;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Salon.Firebase.Database;

namespace Salon.Firebase
{
    public class ChatManager : MonoBehaviour
    {
        private DatabaseReference dbReference;
        private DatabaseReference channelsRef;
        private DatabaseReference currentChannelRef;
        private DatabaseReference currentChannelMessagesRef;
        private Query currentMessagesQuery;
        public Action<string, string, Sprite> OnReceiveChat;
        private string currentChannel;
        private long lastMessageTimestamp;

        public void Initialize(DatabaseReference dbReference)
        {
            this.dbReference = dbReference;
            this.channelsRef = dbReference.Child("Channels");
            lastMessageTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            FirebaseManager.Instance.ChatManager = this;
            Debug.Log("[ChatManager] 초기화 완료");
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

        public void StartListeningToMessages(string channelName)
        {
            try
            {
                StopListeningToMessages();  // 기존 구독 해제
                UpdateChannelReferences(channelName);

                // 최근 메시지부터 구독 시작
                currentMessagesQuery = currentChannelMessagesRef
                    .OrderByChild("Timestamp")
                    .StartAt(lastMessageTimestamp - 1); // 1초 전부터 시작하여 누락 방지

                currentMessagesQuery.ChildAdded += HandleMessageReceived;
                Debug.Log($"[ChatManager] 채널 {channelName}의 메시지 구독 시작 (Timestamp: {lastMessageTimestamp})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 구독 시작 실패: {ex.Message}");
            }
        }

        private async void LoadExistingMessages()
        {
            try
            {
                // 최근 50개의 메시지만 로드
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

                // 이미 처리한 메시지는 스킵
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