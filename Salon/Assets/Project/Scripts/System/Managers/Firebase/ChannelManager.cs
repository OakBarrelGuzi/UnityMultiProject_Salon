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
        private DatabaseReference userRef;
        public string CurrentChannel { get; private set; }
        private string currentUserName;
        private ChatManager chatManager;
        public Action<string, string, Sprite> OnReceiveChat;
        private const float DISCONNECT_TIMEOUT = 5f;
        private DatabaseReference connectedRef;
        private EventHandler<ValueChangedEventArgs> disconnectHandler;

        public async void Initialize()
        {
            try
            {
                Debug.Log("[ChannelManager] Initialize 시작");
                if (dbReference == null)
                {
                    dbReference = await GetDbReference();
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
            // 기존 핸들러 제거
            if (connectedRef != null && disconnectHandler != null)
            {
                connectedRef.ValueChanged -= disconnectHandler;
            }

            // 새로운 핸들러 설정
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
                        // 플레이어 데이터 삭제
                        var playerRef = dbReference.Child("Channels").Child(CurrentChannel)
                            .Child("Players").Child(FirebaseManager.Instance.CurrentUserName);
                        await playerRef.RemoveValueAsync();

                        // UserCount 감소
                        var userCountRef = dbReference.Child("Channels").Child(CurrentChannel)
                            .Child("CommonChannelData").Child("UserCount");

                        var currentCount = await userCountRef.GetValueAsync();
                        int count = currentCount.Value != null ? Convert.ToInt32(currentCount.Value) : 0;
                        var newCount = Math.Max(0, count - 1);

                        var updates = new Dictionary<string, object>
                        {
                            ["UserCount"] = newCount,
                            ["isFull"] = newCount >= 10
                        };

                        await userCountRef.Parent.UpdateChildrenAsync(updates);
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
                var snapshot = await dbReference.Child("Channels").GetValueAsync();
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
                        await dbReference.Child("Channels").Child(roomName).SetRawJsonValueAsync(roomJson);
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

                // 채널 존재 여부 확인
                Debug.Log($"[ChannelManager] 채널 {channelName} 존재 여부 확인 중...");
                var channelSnapshot = await dbReference.Child("Channels").Child(channelName).GetValueAsync();
                if (!channelSnapshot.Exists)
                {
                    Debug.LogError($"[ChannelManager] 채널 {channelName}이 존재하지 않음");
                    throw new Exception($"채널 {channelName}이 존재하지 않습니다.");
                }
                Debug.Log($"[ChannelManager] 채널 {channelName} 존재 확인됨");
                if (CurrentChannel != null)
                {
                    // 현재 채널에서 나가기
                    Debug.Log("[ChannelManager] 현재 채널에서 나가기 시도...");
                    await LeaveChannel(true);
                    Debug.Log("[ChannelManager] 현재 채널에서 나가기 완료");
                }

                // UserCount 확인
                Debug.Log($"[ChannelManager] {channelName} 채널의 유저 수 확인 중...");
                var userCountSnapshot = await dbReference.Child("Channels").Child(channelName)
                    .Child("CommonChannelData").Child("UserCount").GetValueAsync();
                int currentUserCount = userCountSnapshot.Value != null ?
                    Convert.ToInt32(userCountSnapshot.Value) : 0;
                Debug.Log($"[ChannelManager] 현재 유저 수: {currentUserCount}");

                if (currentUserCount >= 10)
                {
                    Debug.LogError("[ChannelManager] 채널이 가득 참");
                    throw new Exception("채널이 가득 찼습니다.");
                }

                CurrentChannel = channelName;
                Debug.Log($"[ChannelManager] CurrentChannel을 {channelName}으로 설정");

                // CurrentUserName 체크
                string currentUserName = FirebaseManager.Instance.CurrentUserName;
                Debug.Log($"[ChannelManager] CurrentUserName: {currentUserName}");
                if (string.IsNullOrEmpty(currentUserName))
                {
                    throw new Exception("[ChannelManager] 현재 사용자 이름이 설정되지 않았습니다.");
                }

                var playerRef = dbReference.Child("Channels").Child(channelName)
                    .Child("Players").Child(currentUserName);

                // 새로운 플레이어 데이터 생성
                Debug.Log("[ChannelManager] 플레이어 데이터 생성 중...");
                var playerData = new GamePlayerData(currentUserName);
                string playerJson = JsonConvert.SerializeObject(playerData);
                Debug.Log($"[ChannelManager] 생성된 플레이어 데이터: {playerJson}");
                await playerRef.SetRawJsonValueAsync(playerJson);
                Debug.Log("[ChannelManager] 플레이어 데이터 저장 완료");

                // UserCount 증가
                Debug.Log("[ChannelManager] UserCount 업데이트 중...");
                await dbReference.Child("Channels").Child(channelName)
                    .Child("CommonChannelData").UpdateChildrenAsync(new Dictionary<string, object>
                    {
                        ["UserCount"] = currentUserCount + 1,
                        ["isFull"] = (currentUserCount + 1) >= 10
                    });
                Debug.Log($"[ChannelManager] UserCount 업데이트 완료: {currentUserCount + 1}");

                // 연결 해제 핸들러 설정
                Debug.Log("[ChannelManager] 연결 해제 핸들러 설정 중...");
                SetupDisconnectHandlers();
                Debug.Log("[ChannelManager] 연결 해제 핸들러 설정 완료");

                // 채팅 구독 시작
                Debug.Log("[ChannelManager] 채팅 구독 시작...");
                chatManager.StartListeningToMessages(channelName);
                Debug.Log("[ChannelManager] 채팅 구독 완료");
                // 입장 메시지 전송

                // 플레이어 프리팹 생성
                RoomManager roomManager = FirebaseManager.Instance.RoomManager;
                if (roomManager != null)
                {
                    Debug.Log("[ChannelManager] 플레이어 프리팹 생성 시작");
                    var newPlayerData = new GamePlayerData(FirebaseManager.Instance.CurrentUserName);
                    roomManager.InstantiatePlayer(FirebaseManager.Instance.CurrentUserName, newPlayerData, isLocalPlayer: true);
                    Debug.Log("[ChannelManager] 플레이어 프리팹 생성 완료");

                    // 플레이어 변경사항 구독 시작
                    Debug.Log("[ChannelManager] 플레이어 변경사항 구독 시작...");
                    await roomManager.SubscribeToPlayerChanges(channelName);
                    Debug.Log("[ChannelManager] 플레이어 변경사항 구독 완료");
                }

                Debug.Log("[ChannelManager] 입장 메시지 전송 중...");
                await chatManager.SendChat($"{currentUserName}님이 입장하셨습니다.", channelName, "System");
                Debug.Log("[ChannelManager] 입장 메시지 전송 완료");

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

                // 채팅 메시지 전송
                if (isNormalDisconnect && chatManager != null)
                {
                    try
                    {
                        Debug.Log($"[ChannelManager] 나가기 메시지 전송 시도 - 채널: {channelName}, 사용자: {currentUserName}");
                        var sendMessageTask = chatManager.SendChat($"{currentUserName}님이 나갔습니다.", channelName, "System");

                        // 5초 타임아웃 설정
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

                // 채팅 구독 중단
                if (chatManager != null)
                {
                    try
                    {
                        Debug.Log("[ChannelManager] 채팅 구독 중단");
                        chatManager.StopListeningToMessages();
                    }
                    catch (Exception chatEx)
                    {
                        Debug.LogError($"[ChannelManager] 채팅 구독 중단 실패: {chatEx.Message}");
                    }
                }

                // RoomManager 정리
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

                CurrentChannel = null;
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
                var snapshot = await dbReference.Child("Channels").GetValueAsync();
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