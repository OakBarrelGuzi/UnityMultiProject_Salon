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
        private string currentChannel;
        public Action<string, string, Sprite> OnReceiveChat;



        public void Initialize(DatabaseReference dbReference)
        {
            this.dbReference = dbReference;
            Debug.Log("[ChatManager] 초기화 완료");
        }

        public async Task SendChat(string message, string channelName, string senderId)
        {
            if (string.IsNullOrEmpty(channelName)) return;

            try
            {
                var messageData = new MessageData(
                    senderId,
                    message,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                );

                string messageKey = dbReference.Child("Channels").Child(channelName)
                    .Child("CommonChannelData").Child("Messages").Push().Key;

                await dbReference.Child("Channels").Child(channelName)
                    .Child("CommonChannelData").Child("Messages")
                    .Child(messageKey).SetRawJsonValueAsync(JsonConvert.SerializeObject(messageData));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 메시지 전송 실패: {ex.Message}");
            }
        }

        public void StartListeningToMessages(string channelName)
        {
            StopListeningToMessages();  // 기존 구독 해제
            currentChannel = channelName;
            LoadExistingMessages();
            SubscribeToNewMessages();
            Debug.Log($"[ChatManager] 채널 {channelName}의 메시지 구독 시작");
        }

        private async void LoadExistingMessages()
        {
            try
            {
                var snapshot = await dbReference.Child("Channels").Child(currentChannel)
                    .Child("CommonChannelData").Child("Messages")
                    .OrderByChild("Timestamp")
                    .GetValueAsync();

                if (snapshot.Exists)
                {
                    foreach (var messageSnapshot in snapshot.Children)
                    {
                        var messageData = JsonConvert.DeserializeObject<MessageData>(messageSnapshot.GetRawJsonValue());
                        OnReceiveChat?.Invoke(messageData.SenderId, messageData.Content, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChatManager] 기존 메시지 로드 실패: {ex.Message}");
            }
        }

        private void SubscribeToNewMessages()
        {
            dbReference.Child("Channels").Child(currentChannel)
                .Child("CommonChannelData").Child("Messages")
                .OrderByChild("Timestamp")
                .StartAt(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                .ChildAdded += HandleMessageReceived;
        }

        public void StopListeningToMessages()
        {
            if (string.IsNullOrEmpty(currentChannel)) return;

            dbReference.Child("Channels").Child(currentChannel)
                .Child("CommonChannelData").Child("Messages")
                .ChildAdded -= HandleMessageReceived;

            currentChannel = null;
            Debug.Log("[ChatManager] 메시지 구독 중단");
        }

        private void HandleMessageReceived(object sender, ChildChangedEventArgs args)
        {
            if (!args.Snapshot.Exists) return;

            try
            {
                var messageData = JsonConvert.DeserializeObject<MessageData>(args.Snapshot.GetRawJsonValue());
                Debug.Log($"[ChatManager] 새 메시지 수신: Sender={messageData.SenderId}, Content={messageData.Content}");
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