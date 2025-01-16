using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Salon.Firebase.Database;
using System.Threading.Tasks;
using Salon.System;
using UnityEngine.SceneManagement;
using System.Threading;

namespace Salon.Firebase
{
    public class ChannelManager : Singleton<ChannelManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference channelsRef;
        private DatabaseReference currentChannelRef;
        private DatabaseReference currentChannelPlayersRef;
        private DatabaseReference currentChannelDataRef;
        private DatabaseReference connectedRef;
        public string CurrentChannel { get; private set; }
        private string currentUserName;
        private const float DISCONNECT_TIMEOUT = 5f;
        private EventHandler<ValueChangedEventArgs> disconnectHandler;

        private bool isQuitting = false;

        public async Task Initialize()
        {
            try
            {
                Debug.Log("[ChannelManager] Initialize 시작");

                // Firebase가 초기화될 때까지 대기
                if (!FirebaseManager.Instance.IsInitialized)
                {
                    Debug.Log("[ChannelManager] Firebase 초기화 대기 중...");
                    await Task.Delay(1000);  // 잠시 대기 후 재시도
                    if (!FirebaseManager.Instance.IsInitialized)
                    {
                        throw new Exception("Firebase가 초기화되지 않았습니다.");
                    }
                }

                if (dbReference == null)
                {
                    dbReference = await GetDbReference();
                    if (dbReference == null)
                    {
                        throw new Exception("데이터베이스 참조를 가져올 수 없습니다.");
                    }
                    currentUserName = FirebaseManager.Instance.CurrentUserName;

                    channelsRef = dbReference.Child("Channels");
                    Debug.Log("[ChannelManager] 데이터베이스 참조 설정 완료");

                    SetupDisconnectHandlers();
                    await ExistRooms();  // 채널이 존재하는지 확인하고 필요한 경우 생성
                }

                Debug.Log("[ChannelManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 초기화 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
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
                        await LeaveChannel(false);
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

                await FriendManager.Instance.Initialize();

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

                // 온라인 상태 설정 및 OnDisconnect 핸들러 등록
                await SetupOnlineStatus();

                await ValidateChannelCapacity();

                await SetupPlayerData();

                await Task.WhenAll(
                    ChatManager.Instance.StartListeningToMessages(channelName),
                    RoomManager.Instance.JoinChannel(channelName)
                );

                Debug.Log($"[ChannelManager] {channelName} 채널 입장 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 입장 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        private async Task SetupOnlineStatus()
        {
            try
            {
                string currentUserPath = $"Users/{FirebaseManager.Instance.CurrentUserName}";
                var userStatusRef = FirebaseManager.Instance.DbReference.Child(currentUserPath).Child("Status");

                // 현재 상태를 온라인으로 설정
                await userStatusRef.SetValueAsync("online");
                Debug.Log("[ChannelManager] 온라인 상태 설정 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 온라인 상태 설정 실패: {ex.Message}");
                throw;
            }
        }

        private async Task ValidateChannelCapacity()
        {
            try
            {
                var currentCount = await GetChannelUserCount(CurrentChannel);
                if (currentCount >= 10)
                {
                    throw new Exception("채널이 가득 찼습니다.");
                }
                Debug.Log($"[ChannelManager] 채널 {CurrentChannel} 입장 가능 (현재 인원: {currentCount}/10)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 수용 인원 확인 실패: {ex.Message}");
                throw;
            }
        }

        private async Task SetupPlayerData()
        {
            string currentUserName = FirebaseManager.Instance.CurrentUserName;
            if (string.IsNullOrEmpty(currentUserName))
            {
                throw new Exception("[ChannelManager] 현재 사용자 이름이 설정되지 않았습니다.");
            }

            var playerData = new GamePlayerData(currentUserName);
            await AddPlayerToChannel(CurrentChannel, currentUserName, playerData);
        }

        public async Task LeaveChannel(bool isDisconnecting = false)
        {
            try
            {
                if (CurrentChannel == null) return;

                Debug.Log($"[ChannelManager] 채널 나가기 시작: {CurrentChannel}");

                ChatManager.Instance.StopListeningToMessages();
                RoomManager.Instance.UnsubscribeFromChannel();

                if (currentChannelPlayersRef != null)
                {
                    try
                    {
                        await currentChannelPlayersRef.RemoveValueAsync();
                    }
                    catch (Exception)
                    {

                    }
                }

                if (!isDisconnecting)
                {
                    try
                    {
                        string currentUserPath = $"Users/{FirebaseManager.Instance.CurrentUserName}";
                        var userStatusRef = FirebaseManager.Instance.DbReference.Child(currentUserPath).Child("Status");
                        await userStatusRef.SetValueAsync("offline");
                    }
                    catch (Exception)
                    {

                    }
                }

                FriendManager.Instance.Cleanup();

                CurrentChannel = null;
                Debug.Log("[ChannelManager] 채널 나가기 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 나가기 실패: {ex.Message}");
                CurrentChannel = null;
                throw;
            }
        }

        private async void OnDestroy()
        {
            if (!Application.isPlaying || isQuitting) return;

            try
            {
                Debug.Log("[ChannelManager] OnDestroy 시작");
                await LeaveChannel(false);
                Debug.Log("[ChannelManager] OnDestroy 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] OnDestroy 처리 실패: {ex.Message}");
            }
        }

        private async void OnApplicationQuit()
        {
            if (isQuitting) return;
            isQuitting = true;

            try
            {
                Debug.Log("[ChannelManager] OnApplicationQuit 시작");
                await LeaveChannel(false);
                Debug.Log("[ChannelManager] OnApplicationQuit 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] OnApplicationQuit 처리 실패: {ex.Message}");
            }
        }

        private void CleanupResources()
        {
            try
            {
                if (connectedRef != null && disconnectHandler != null)
                {
                    connectedRef.ValueChanged -= disconnectHandler;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 리소스 정리 실패: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Application.quitting += OnApplicationQuit;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.quitting -= OnApplicationQuit;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[ChannelManager] 씬 로드됨: {scene.name}");

            if (!FirebaseManager.Instance.IsInitialized)
            {
                Debug.Log("[ChannelManager] Firebase 재초기화 필요");
                _ = Initialize();
            }
        }

        public async Task<Dictionary<string, ChannelData>> WaitForChannelData()
        {
            try
            {
                if (channelsRef == null)
                {
                    Debug.LogWarning("[ChannelManager] 채널 참조가 초기화되지 않았습니다. 초기화를 시도합니다.");
                    await Initialize();

                    if (channelsRef == null)
                    {
                        Debug.LogError("[ChannelManager] 채널 참조 초기화 실패");
                        return new Dictionary<string, ChannelData>();
                    }
                }

                var snapshot = await channelsRef.GetValueAsync();
                if (!snapshot.Exists)
                {
                    Debug.LogWarning("[ChannelManager] 채널 데이터가 존재하지 않습니다.");
                    return new Dictionary<string, ChannelData>();
                }

                var channelData = new Dictionary<string, ChannelData>();
                foreach (var channelSnapshot in snapshot.Children)
                {
                    try
                    {
                        var data = JsonConvert.DeserializeObject<ChannelData>(channelSnapshot.GetRawJsonValue());
                        if (data != null)
                        {
                            channelData[channelSnapshot.Key] = data;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ChannelManager] 채널 {channelSnapshot.Key} 데이터 파싱 실패: {ex.Message}");
                        continue;
                    }
                }
                return channelData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 데이터 로드 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                return new Dictionary<string, ChannelData>();
            }
        }

        public async Task<int> GetChannelUserCount(string channelName)
        {
            try
            {
                var playersSnapshot = await channelsRef.Child(channelName).Child("Players").GetValueAsync();
                return (int)playersSnapshot.ChildrenCount;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 유저 수 조회 실패: {ex.Message}");
                throw;
            }
        }

        public async Task RemovePlayerFromChannel(string channelName, string playerName)
        {
            try
            {
                var playerRef = channelsRef.Child(channelName).Child("Players").Child(playerName);
                await playerRef.RemoveValueAsync();
                Debug.Log($"[ChannelManager] 플레이어 {playerName} 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 플레이어 제거 실패: {ex.Message}");
                throw;
            }
        }

        public async Task AddPlayerToChannel(string channelName, string playerName, GamePlayerData playerData)
        {
            try
            {
                var currentCount = await GetChannelUserCount(channelName);
                if (currentCount >= 10)
                {
                    throw new Exception("채널이 가득 찼습니다.");
                }

                // 플레이어 데이터 추가
                var playerRef = channelsRef.Child(channelName).Child("Players").Child(playerName);
                await playerRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(playerData));

                // 연결이 끊어질 때 플레이어 데이터 자동 제거
                await playerRef.OnDisconnect().RemoveValue();

                Debug.Log($"[ChannelManager] 플레이어 {playerName} 추가 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 플레이어 추가 실패: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsChannelFull(string channelName)
        {
            try
            {
                var currentCount = await GetChannelUserCount(channelName);
                return currentCount >= 10;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChannelManager] 채널 가득 참 여부 확인 실패: {ex.Message}");
                throw;
            }
        }
    }
}