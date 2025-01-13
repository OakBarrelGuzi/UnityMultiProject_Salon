using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Salon.Firebase.Database;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Salon.Firebase
{
    public class ChannelManager : MonoBehaviour
    {
        private DatabaseReference dbReference;
        private DatabaseReference channelsRef;
        private DatabaseReference currentChannelRef;
        private DatabaseReference currentChannelPlayersRef;
        private DatabaseReference currentChannelDataRef;
        private DatabaseReference connectedRef;
        public string CurrentChannel { get; private set; }
        private string currentUserName;
        private ChatManager chatManager;
        public Action<string, string, Sprite> OnReceiveChat;
        private const float DISCONNECT_TIMEOUT = 5f;
        private EventHandler<ValueChangedEventArgs> disconnectHandler;

        public async void Initialize()
        {
            try
            {
                Debug.Log("[ChannelManager] Initialize 시작");
                if (dbReference == null)
                {
                    dbReference = await GetDbReference();
                    channelsRef = dbReference.Child("Channels");
                    Debug.Log("[ChannelManager] 데이터베이스 참조 설정 완료");
                    SetupDisconnectHandlers();
                }

                if (chatManager == null)
                {
                    GameObject chatObj = new GameObject("ChatManager");
                    chatManager = chatObj.AddComponent<ChatManager>();
                    chatObj.transform.SetParent(transform);
                    chatManager.Initialize(dbReference);
                    chatManager.OnReceiveChat += (sender, message, sprite) => OnReceiveChat?.Invoke(sender, message, sprite);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 초기화 실패: {ex.Message}");
            }
        }

        private void UpdateChannelReferences(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return;

            currentChannelRef = channelsRef.Child(channelName);
            currentChannelPlayersRef = currentChannelRef.Child("Players");
            currentChannelDataRef = currentChannelRef.Child("CommonChannelData");
            CurrentChannel = channelName;
            Debug.Log($"[ChannelManager] 채널 레퍼런스 업데이트 완료: {channelName}");
        }

        private void ClearChannelReferences()
        {
            currentChannelRef = null;
            currentChannelPlayersRef = null;
            currentChannelDataRef = null;
            CurrentChannel = null;
            Debug.Log("[ChannelManager] 채널 레퍼런스 초기화 완료");
        }

        public void SetCurrentUserName(string userName)
        {
            currentUserName = userName;
            Debug.Log($"[ChannelManager] 현재 사용자 이름 설정: {currentUserName}");
        }

        public async Task SendChat(string message)
        {
            if (string.IsNullOrEmpty(CurrentChannel)) return;
            await chatManager.SendChat(message, CurrentChannel, FirebaseManager.Instance.GetCurrentDisplayName());
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

                Debug.Log($"[ChannelManager] Firebase 데이터베이스 참조 대기 중... (시도 {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("[ChannelManager] Firebase 데이터베이스 참조를 가져올 수 없습니다.");
        }

        private void SetupDisconnectHandlers()
        {
            if (connectedRef != null && disconnectHandler != null)
            {
                connectedRef.ValueChanged -= disconnectHandler;
            }

            connectedRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
            disconnectHandler = async (sender, args) =>
            {
                try
                {
                    if (args.Snapshot.Value == null) return;
                    bool isConnected = (bool)args.Snapshot.Value;

                    if (!isConnected && !string.IsNullOrEmpty(CurrentChannel) &&
                        FirebaseManager.Instance.CurrentUserName != null)
                    {
                        Debug.Log("[ChannelManager] 연결 끊김 감지, 정리 작업 시작");

                        if (currentChannelPlayersRef != null)
                        {
                            var playerRef = currentChannelPlayersRef.Child(FirebaseManager.Instance.CurrentUserName);
                            await playerRef.RemoveValueAsync();
                        }

                        if (currentChannelDataRef != null)
                        {
                            var userCountRef = currentChannelDataRef.Child("UserCount");
                            var currentCount = await userCountRef.GetValueAsync();
                            int count = currentCount.Value != null ? Convert.ToInt32(currentCount.Value) : 0;
                            var newCount = Math.Max(0, count - 1);

                            var updates = new Dictionary<string, object>
                            {
                                ["UserCount"] = newCount,
                                ["isFull"] = newCount >= 10
                            };

                            await userCountRef.Parent.UpdateChildrenAsync(updates);
                        }

                        Debug.Log("[ChannelManager] 연결 끊김 정리 작업 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ChannelManager] 연결 상태 처리 중 오류 발생: {ex.Message}");
                }
            };

            connectedRef.ValueChanged += disconnectHandler;
            Debug.Log("[ChannelManager] 연결 해제 핸들러 설정 완료");
        }

        public async Task ExistRooms()
        {
            try
            {
                var snapshot = await channelsRef.GetValueAsync();
                var existingRooms = new HashSet<string>();

                if (snapshot.Exists)
                {
                    foreach (var room in snapshot.Children)
                    {
                        existingRooms.Add(room.Key);
                    }
                }

                await CreateMissingRooms(existingRooms);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 목록 확인 실패: {ex.Message}");
            }
        }

        private async Task CreateMissingRooms(HashSet<string> existingRooms)
        {
            for (int i = 1; i <= 10; i++)
            {
                string roomName = $"Channel{i:D2}";
                if (!existingRooms.Contains(roomName))
                {
                    try
                    {
                        var roomData = new ChannelData();
                        string roomJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                        await channelsRef.Child(roomName).SetRawJsonValueAsync(roomJson);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ChannelManager] 방 {roomName} 생성 중 오류: {ex.Message}");
                    }
                }
            }
        }

        public async Task JoinChannel(string channelName)
        {
            try
            {
                Debug.Log($"[ChannelManager] {channelName} 채널 입장 시도 시작");

                var channelSnapshot = await channelsRef.Child(channelName).GetValueAsync();
                if (!channelSnapshot.Exists)
                {
                    Debug.LogError($"[ChannelManager] 채널 {channelName}이 존재하지 않음");
                    throw new Exception($"채널 {channelName}이 존재하지 않습니다.");
                }

                if (CurrentChannel != null)
                {
                    Debug.Log("[ChannelManager] 현재 채널에서 나가기 시도...");
                    await LeaveChannel(true);
                    Debug.Log("[ChannelManager] 현재 채널에서 나가기 완료");
                }

                UpdateChannelReferences(channelName);

                var userCountSnapshot = await currentChannelDataRef.Child("UserCount").GetValueAsync();
                int currentUserCount = userCountSnapshot.Value != null ?
                    Convert.ToInt32(userCountSnapshot.Value) : 0;

                if (currentUserCount >= 10)
                {
                    Debug.LogError("[ChannelManager] 채널이 가득 참");
                    throw new Exception("채널이 가득 찼습니다.");
                }

                string currentUserName = FirebaseManager.Instance.CurrentUserName;
                if (string.IsNullOrEmpty(currentUserName))
                {
                    throw new Exception("[ChannelManager] 현재 사용자 이름이 설정되지 않았습니다.");
                }

                var playerRef = currentChannelPlayersRef.Child(currentUserName);
                var playerData = new GamePlayerData(currentUserName);
                string playerJson = JsonConvert.SerializeObject(playerData);
                await playerRef.SetRawJsonValueAsync(playerJson);

                await currentChannelDataRef.UpdateChildrenAsync(new Dictionary<string, object>
                {
                    ["UserCount"] = currentUserCount + 1,
                    ["isFull"] = (currentUserCount + 1) >= 10
                });

                SetupDisconnectHandlers();
                chatManager.StartListeningToMessages(channelName);

                RoomManager roomManager = FirebaseManager.Instance.RoomManager;
                if (roomManager != null)
                {
                    var newPlayerData = new GamePlayerData(FirebaseManager.Instance.CurrentUserName);
                    roomManager.InstantiatePlayer(FirebaseManager.Instance.CurrentUserName, newPlayerData, isLocalPlayer: true);
                    await roomManager.SubscribeToPlayerChanges(channelName);
                }

                await chatManager.SendChat($"{currentUserName}님이 입장하셨습니다.", channelName, "System");
                Debug.Log($"[ChannelManager] {channelName} 채널 입장 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 입장 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        public async Task LeaveChannel(bool isNormalDisconnect = true)
        {
            try
            {
                string channelName = CurrentChannel;
                Debug.Log($"[ChannelManager] LeaveChannel 시작 - Channel: {channelName}, User: {currentUserName}");

                if (string.IsNullOrEmpty(channelName))
                {
                    Debug.Log("[ChannelManager] CurrentChannel이 null이거나 비어있어 종료");
                    return;
                }

                if (isNormalDisconnect && chatManager != null)
                {
                    try
                    {
                        var sendMessageTask = chatManager.SendChat($"{currentUserName}님이 나갔습니다.", channelName, "System");
                        var timeoutTask = Task.Delay(5000);
                        var completedTask = await Task.WhenAny(sendMessageTask, timeoutTask);

                        if (completedTask == sendMessageTask)
                        {
                            await sendMessageTask;
                            Debug.Log("[ChannelManager] 나가기 메시지 전송 완료");
                        }
                        else
                        {
                            Debug.LogWarning("[ChannelManager] 나가기 메시지 전송 시간 초과");
                        }
                    }
                    catch (Exception chatEx)
                    {
                        Debug.LogError($"[ChannelManager] 나가기 메시지 전송 실패: {chatEx.Message}");
                    }
                }

                if (chatManager != null)
                {
                    chatManager.StopListeningToMessages();
                }

                var roomManager = FirebaseManager.Instance?.RoomManager;
                if (roomManager != null)
                {
                    try
                    {
                        Debug.Log("[ChannelManager] RoomManager 정리 시작");
                        roomManager.UnsubscribeFromChannel();
                        roomManager.DestroyAllPlayers();
                        Debug.Log("[ChannelManager] RoomManager 정리 완료");
                    }
                    catch (Exception roomEx)
                    {
                        Debug.LogError($"[ChannelManager] RoomManager 정리 실패: {roomEx.Message}");
                    }
                }

                ClearChannelReferences();
                Debug.Log($"[ChannelManager] 채널 나가기 완료 - 이전 채널: {channelName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 나가기 실패: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<Dictionary<string, ChannelData>> WaitForChannelData()
        {
            try
            {
                var snapshot = await channelsRef.GetValueAsync();
                if (!snapshot.Exists) return null;

                var channelData = new Dictionary<string, ChannelData>();
                foreach (var channelSnapshot in snapshot.Children)
                {
                    channelData[channelSnapshot.Key] = JsonConvert.DeserializeObject<ChannelData>(channelSnapshot.GetRawJsonValue());
                }
                return channelData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 데이터 로드 실패: {ex.Message}");
                return null;
            }
        }
    }
}