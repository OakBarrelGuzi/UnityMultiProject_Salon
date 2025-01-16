using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Salon.Interfaces;
using UnityEngine.SceneManagement;
using Salon.System;
using Salon.Firebase.Database;

namespace Salon.Firebase
{
    public class FirebaseManager : Singleton<FirebaseManager>, IInitializable
    {
        private DatabaseReference dbReference;
        public DatabaseReference DbReference { get => dbReference; }
        private FirebaseAuth auth;
        private FirebaseUser currentUser;

        private bool isConnected = false;
        private DatabaseReference connectionRef;

        private string currentUserName;
        public string CurrentUserName => currentUserName;

        public bool IsInitialized { get; private set; }
        private TaskCompletionSource<bool> initializationComplete;

        private bool isChannelManagerInitialized;
        private bool isChatManagerInitialized;
        private bool isRoomManagerInitialized;
        private bool isFriendManagerInitialized;

        private bool isInitializing = false;

        void Start()
        {
            if (!IsInitialized && !isInitializing)
            {
                Initialize();
            }
        }

        public async void Initialize()
        {
            if (IsInitialized || isInitializing)
            {
                Debug.Log("[FirebaseManager] 이미 초기화되었거나 초기화 중입니다.");
                return;
            }

            isInitializing = true;

            try
            {
                initializationComplete = new TaskCompletionSource<bool>();
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebase();

                    // 사용자 정보 먼저 설정
                    if (auth != null)
                    {
                        currentUser = auth.CurrentUser;
                        if (currentUser != null)
                        {
                            currentUserName = currentUser.DisplayName;
                            Debug.Log("[FirebaseManager] 사용자 이름: " + currentUserName + "/ 이메일 : " + currentUser.Email);
                        }
                        else
                        {
                            Debug.Log("[FirebaseManager] 로그인된 사용자가 없습니다.");
                        }
                    }

                    IsInitialized = true;
                    initializationComplete.SetResult(true);

                    if (currentUser != null)
                    {
                        await UpdateUserStatus(UserStatus.Online);
                    }

                    var currentUserRef = dbReference.Child("Users").Child(currentUserName);
                    await currentUserRef.OnDisconnect().UpdateChildren(new Dictionary<string, object> { { "IsOnline", false } });

                    await InitializeManagers();
                    Debug.Log("[FirebaseManager] Firebase 초기화 성공");
                }
                else
                {
                    Debug.LogError($"[FirebaseManager] Firebase 초기화 실패: {dependencyStatus}");
                    initializationComplete.SetResult(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 초기화 실패: {ex.Message}");
                initializationComplete.SetResult(false);
            }
            finally
            {
                isInitializing = false;
            }
        }

        private async Task InitializeManagers()
        {
            try
            {
                if (!isChannelManagerInitialized)
                {
                    await ChannelManager.Instance.Initialize();
                    isChannelManagerInitialized = true;
                    Debug.Log("[FirebaseManager] ChannelManager 초기화 완료");
                }

                if (!isChatManagerInitialized)
                {
                    await ChatManager.Instance.Initialize();
                    isChatManagerInitialized = true;
                    Debug.Log("[FirebaseManager] ChatManager 초기화 완료");
                }

                if (!isRoomManagerInitialized)
                {
                    await RoomManager.Instance.Initialize();
                    isRoomManagerInitialized = true;
                    Debug.Log("[FirebaseManager] RoomManager 초기화 완료");
                }

                if (!isFriendManagerInitialized)
                {
                    await FriendManager.Instance.Initialize();
                    isFriendManagerInitialized = true;
                    Debug.Log("[FirebaseManager] FriendManager 초기화 완료");
                }

                Debug.Log("[FirebaseManager] 모든 매니저 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 매니저 초기화 실패: {ex.Message}");
                throw;
            }
        }

        private async Task EnsureInitialized()
        {
            if (!IsInitialized)
            {
                if (initializationComplete == null)
                {
                    Initialize();
                }
                await initializationComplete.Task;
            }
        }

        private void InitializeFirebase()
        {
            auth = FirebaseAuth.DefaultInstance;
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;

            connectionRef = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
            connectionRef.ValueChanged += HandleConnectionChanged;

            auth.StateChanged += AuthStateChanged;
        }

        private async void AuthStateChanged(object sender, EventArgs e)
        {
            if (auth.CurrentUser != currentUser)
            {
                bool signedIn = (auth.CurrentUser != null);
                if (signedIn)
                {
                    Debug.Log($"[FirebaseManager] 사용자 로그인: {auth.CurrentUser.Email}");
                    currentUser = auth.CurrentUser;
                    currentUserName = currentUser.DisplayName;
                    Debug.Log($"[FirebaseManager] 현재 사용자 이름: {currentUserName}");

                    if (ChannelManager.Instance != null)
                    {
                        await ChannelManager.Instance.Initialize();
                        Debug.Log("[FirebaseManager] ChannelManager 레퍼런스 업데이트 완료");

                    }

                    if (ChatManager.Instance != null)
                    {
                        await ChatManager.Instance.Initialize();
                        Debug.Log("[FirebaseManager] ChatManager 레퍼런스 업데이트 완료");
                    }

                    if (RoomManager.Instance != null)
                    {
                        await RoomManager.Instance.Initialize();
                        Debug.Log("[FirebaseManager] RoomManager 레퍼런스 업데이트 완료");
                    }

                    if (FriendManager.Instance != null)
                    {
                        await FriendManager.Instance.Initialize();
                        Debug.Log("[FirebaseManager] FriendManager 레퍼런스 업데이트 완료");
                    }
                }
                else
                {
                    Debug.Log("[FirebaseManager] 사용자 로그아웃");
                    currentUser = null;
                    currentUserName = null;

                    // 각 매니저의 초기화 상태 리셋
                    isChannelManagerInitialized = false;
                    isChatManagerInitialized = false;
                    isRoomManagerInitialized = false;
                    isFriendManagerInitialized = false;
                }
            }
        }

        private void HandleConnectionChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[FirebaseManager] 연결 상태 확인 오류: {args.DatabaseError.Message}");
                return;
            }

            isConnected = (bool)args.Snapshot.Value;
            if (isConnected)
            {
                Debug.Log("[FirebaseManager] 연결됨");
            }
            else
            {
                Debug.LogWarning("[FirebaseManager] 연결 끊김, 재연결 시도...");
                ReconnectFirebase();
            }
        }

        private async void ReconnectFirebase()
        {
            try
            {
                FirebaseDatabase.DefaultInstance.GoOnline();
                await Task.Delay(1000); // 재연결 대기

                if (!isConnected)
                {
                    Debug.Log("[FirebaseManager] 재초기화 시도");
                    Initialize();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 재연결 실패: {ex.Message}");
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                Debug.Log("[FirebaseManager] 앱 일시정지, 연결 해제");
                _ = UpdateUserStatus(UserStatus.Away);
                FirebaseDatabase.DefaultInstance.GoOffline();
            }
            else
            {
                Debug.Log("[FirebaseManager] 앱 재개, 연결 복구");
                FirebaseDatabase.DefaultInstance.GoOnline();
                _ = UpdateUserStatus(UserStatus.Online);
            }
        }

        bool isQuitting = false;

        private async void OnApplicationQuit()
        {
            if (!Application.isPlaying || isQuitting) return;

            try
            {
                isQuitting = true;
                Debug.Log("[FirebaseManager] OnApplicationQuit 시작");
                await UpdateUserStatus(UserStatus.Offline);
                auth.StateChanged -= AuthStateChanged;
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Debug.Log("[FirebaseManager] OnApplicationQuit 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] OnApplicationQuit 처리 실패: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying || isQuitting) return;

            try
            {
                Debug.Log("[FirebaseManager] OnDestroy 시작");
                isQuitting = true;

                // 이벤트 핸들러 제거
                if (connectionRef != null)
                {
                    connectionRef.ValueChanged -= HandleConnectionChanged;
                }
                if (auth != null)
                {
                    auth.StateChanged -= AuthStateChanged;
                }

                SceneManager.sceneLoaded -= OnSceneLoaded;

                Debug.Log("[FirebaseManager] 핸들러 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] OnDestroy 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task<string> GenerateUniqueTagAsync(string baseName)
        {
            try
            {
                Debug.Log($"[FirebaseManager] 고유 태그 생성 시작 - 기본 이름: {baseName}");
                await EnsureInitialized();
                if (!IsInitialized)
                {
                    Debug.LogError("[FirebaseManager] Firebase가 초기화되지 않았습니다.");
                    throw new InvalidOperationException("[FirebaseManager] Firebase가 초기화되지 않았습니다.");
                }

                if (string.IsNullOrEmpty(baseName))
                {
                    Debug.LogError("[FirebaseManager] 기본 이름이 비어있습니다.");
                    throw new ArgumentException("[FirebaseManager] 기본 이름이 비어있습니다.");
                }

                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                const int tagLength = 4;

                int maxAttempts = 100;
                int attempts = 0;

                while (attempts < maxAttempts)
                {
                    char[] tagArray = new char[tagLength];
                    for (int i = 0; i < tagLength; i++)
                    {
                        tagArray[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
                    }
                    string randomTag = new string(tagArray);
                    string uniqueTag = $"{baseName}_{randomTag}";

                    Debug.Log($"[FirebaseManager] 태그 생성 시도 {attempts + 1}: {uniqueTag}");

                    var snapshot = await dbReference.Child("Users").Child(uniqueTag).GetValueAsync();
                    if (!snapshot.Exists)
                    {
                        Debug.Log($"[FirebaseManager] 고유한 태그 생성 성공: {uniqueTag} (시도 횟수: {attempts + 1})");
                        return uniqueTag;
                    }

                    Debug.Log($"[FirebaseManager] 태그 {uniqueTag}가 이미 존재함, 재시도...");
                    attempts++;
                }

                throw new Exception($"[FirebaseManager] {maxAttempts}번의 시도 후에도 고유한 태그를 생성하지 못했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 태그 생성 실패: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> RegisterWithEmailAsync(string email, string password)
        {
            try
            {
                await EnsureInitialized();
                if (!IsInitialized)
                {
                    Debug.LogError("[FirebaseManager] Firebase가 초기화되지 않았습니다.");
                    return false;
                }

                Debug.Log($"[FirebaseManager] 회원가입 시도: {email}");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    Debug.LogError("[FirebaseManager] 이메일 또는 비밀번호가 비어있습니다.");
                    return false;
                }

                if (password.Length < 6)
                {
                    Debug.LogError("[FirebaseManager] 비밀번호는 최소 6자 이상이어야 합니다.");
                    return false;
                }

                Debug.Log("[FirebaseManager] 사용자 생성 시도...");
                var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

                if (result == null || result.User == null)
                {
                    Debug.LogError("[FirebaseManager] 사용자 생성 결과가 null입니다.");
                    return false;
                }

                Debug.Log("[FirebaseManager] 고유 태그 생성 시도...");
                string uniqueDisplayName = await GenerateUniqueTagAsync("user");
                Debug.Log($"[FirebaseManager] 생성된 태그: {uniqueDisplayName}");

                Debug.Log("[FirebaseManager] 프로필 업데이트 시도...");
                var profile = new UserProfile { DisplayName = uniqueDisplayName };
                await result.User.UpdateUserProfileAsync(profile);
                Debug.Log("[FirebaseManager] 프로필 업데이트 완료");

                try
                {
                    Debug.Log("[FirebaseManager] 사용자 데이터 생성 시도...");

                    var userData = new Database.UserData
                    {
                        LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        IsOnline = true,
                        Status = UserStatus.Online,
                        Friends = new Dictionary<string, bool>(),
                        GameStats = new Dictionary<GameType, UserStats>(),
                        Invites = new Dictionary<string, InviteData>()
                    };

                    Debug.Log("[FirebaseManager] 데이터베이스에 사용자 데이터 저장 시도...");
                    string jsonData = JsonConvert.SerializeObject(userData);
                    await dbReference.Child("Users").Child(uniqueDisplayName).SetRawJsonValueAsync(jsonData);
                    Debug.Log("[FirebaseManager] 사용자 데이터 저장 완료");

                    Debug.Log("[FirebaseManager] 모든 데이터베이스 작업 완료");
                    Debug.Log($"[FirebaseManager] 회원가입 성공: {result.User.Email} ({uniqueDisplayName})");
                    return true;
                }
                catch (Exception dbEx)
                {
                    Debug.LogError($"[FirebaseManager] 데이터베이스 작업 실패 상세: {dbEx.GetType().Name} - {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Debug.LogError($"[FirebaseManager] 내부 예외: {dbEx.InnerException.Message}");
                        Debug.LogError($"[FirebaseManager] 내부 예외 스택 트레이스: {dbEx.InnerException.StackTrace}");
                    }
                    Debug.LogError($"[FirebaseManager] 스택 트레이스: {dbEx.StackTrace}");
                    return true;
                }
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[FirebaseManager] Firebase 예외: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 일반 예외: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SignInWithEmailAsync(string email, string password)
        {
            try
            {
                var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                currentUserName = result.User.DisplayName;
                await UpdateUserLastOnline(currentUserName);
                Debug.Log($"[FirebaseManager] 로그인 성공: {result.User.Email} (DisplayName: {currentUserName})");

                // ChannelManager에 현재 사용자 이름 설정
                if (ChannelManager.Instance != null)
                {
                    ChannelManager.Instance.SetCurrentUserName(currentUserName);
                }
                else
                {
                    Debug.LogError("[FirebaseManager] ChannelManager가 null입니다");
                }

                await ChannelManager.Instance.ExistRooms();
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.Log($"[FirebaseManager] 오류 코드: {ex.ErrorCode}");
                switch (ex.ErrorCode)
                {
                    case 17011:
                        LogManager.Instance.ShowLog("존재하지 않는 이메일입니다.");
                        break;
                    case 17009:
                        LogManager.Instance.ShowLog("비밀번호가 올바르지 않습니다.");
                        break;
                    case 17020:
                        LogManager.Instance.ShowLog("네트워크 연결을 확인해주세요.");
                        break;
                    case -2:
                        LogManager.Instance.ShowLog("서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
                        break;
                    default:
                        LogManager.Instance.ShowLog("로그인에 실패했습니다. 다시 시도해주세요.");
                        break;
                }
                return false;
            }
        }

        private async Task UpdateUserLastOnline(string displayName)
        {
            try
            {
                var userData = await GetUserDataAsync(displayName) ?? new Database.UserData();
                userData.LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                userData.IsOnline = true;
                userData.Status = UserStatus.Online;

                string jsonData = JsonConvert.SerializeObject(userData);
                await dbReference.Child("Users").Child(displayName).SetRawJsonValueAsync(jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 마지막 접속 시간 업데이트 실패: {ex.Message}");
            }
        }

        public async Task<Database.UserData> GetUserDataAsync(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                Debug.LogWarning("[FirebaseManager] displayName이 null이거나 비어있습니다.");
                return null;
            }

            try
            {
                var snapshot = await dbReference.Child("Users").Child(displayName).GetValueAsync();
                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[FirebaseManager] 사용자 {displayName}의 데이터가 없습니다.");
                    return null;
                }

                var rawJson = snapshot.GetRawJsonValue();
                if (string.IsNullOrEmpty(rawJson))
                {
                    Debug.LogWarning($"[FirebaseManager] 사용자 {displayName}의 JSON 데이터가 비어있습니다.");
                    return null;
                }

                return JsonConvert.DeserializeObject<Database.UserData>(rawJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 사용자 정보 조회 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                return null;
            }
        }

        public void SignOut()
        {
            try
            {
                auth.StateChanged -= AuthStateChanged;

                if (SceneManager.GetActiveScene().name != "MainScene")
                {
                    Debug.Log("[Firebase Manager] MainScene으로 이동");
                    SceneManager.LoadScene("MainScene");
                }

                Debug.Log("[Firebase Manager] 로그아웃 처리 시작");
                auth.SignOut();
                currentUser = null;
                currentUserName = null;
                Debug.Log("[Firebase Manager] 로그아웃 처리 완료");

                auth.StateChanged += AuthStateChanged;

                LogManager.Instance.ShowLog("[Firebase Manager] 로그아웃 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase Manager] 로그아웃 중 오류 발생: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                LogManager.Instance.ShowLog("로그아웃 중 오류가 발생했습니다.");

                auth.StateChanged += AuthStateChanged;
            }
        }

        public async void InitializeRooms()
        {
            try
            {
                Debug.Log("[Firebase Manager] 방 초기화 시작");
                var snapshot = await dbReference.Child("Rooms").GetValueAsync();

                HashSet<string> existingRooms = new HashSet<string>();
                foreach (var room in snapshot.Children)
                {
                    existingRooms.Add(room.Key);
                }

                await CreateMissingRoomsAsync(existingRooms);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase Manager] 방 초기화 실패: {ex.Message}");
            }
        }

        private async Task CreateMissingRoomsAsync(HashSet<string> existingRooms)
        {
            Dictionary<string, Database.ChannelData> rooms = new Dictionary<string, Database.ChannelData>();

            for (int i = 1; i <= 10; i++)
            {
                string roomName = $"Room{i}";
                if (!existingRooms.Contains(roomName))
                {
                    rooms[roomName] = new Database.ChannelData();
                }
            }

            if (rooms.Count > 0)
            {
                try
                {
                    var updates = new Dictionary<string, object>();
                    foreach (var room in rooms)
                    {
                        updates[room.Key] = JsonConvert.SerializeObject(room.Value);
                    }

                    await dbReference.Child("Rooms").UpdateChildrenAsync(updates);
                    Debug.Log($"[Firebase Manager] {rooms.Count}개의 방방 생성 완료");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Firebase Manager] 방 생성 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("[Firebase Manager] 모든 방이 이미 존재함");
            }
        }

        public async Task<bool> UpdateDisplayNameAsync(string newDisplayName)
        {
            if (currentUser == null) return false;

            try
            {
                string uniqueDisplayName = await GenerateUniqueTagAsync(newDisplayName);

                var profile = new UserProfile { DisplayName = uniqueDisplayName };
                await currentUser.UpdateUserProfileAsync(profile);

                var updates = new Dictionary<string, object>
                {
                    ["DisplayName"] = uniqueDisplayName
                };
                await dbReference.Child("Users").Child(currentUser.UserId).UpdateChildrenAsync(updates);

                Debug.Log($"[Firebase Manager] 디디스플레이 네임 변경 성공: {uniqueDisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase Manager] 디스플레이 네임 변경 실패: {ex.Message}");
                return false;
            }
        }

        public string GetCurrentDisplayName()
        {
            return currentUser?.DisplayName ?? "Unknown";
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[Firebase Manager] 씬 로드됨: {scene.name}");

            if (!isConnected || !IsInitialized || DbReference == null)
            {
                Debug.Log("[Firebase Manager] 재연결 시도");
                ReconnectFirebase();
            }
        }

        public async Task UpdateUserStatus(UserStatus status)
        {
            if (currentUser == null || string.IsNullOrEmpty(currentUserName)) return;

            try
            {
                var userData = await GetUserDataAsync(currentUserName) ?? new Database.UserData();
                userData.LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                userData.Status = status;
                userData.IsOnline = status != UserStatus.Offline;

                string jsonData = JsonConvert.SerializeObject(userData);
                await dbReference.Child("Users").Child(currentUserName).SetRawJsonValueAsync(jsonData);

                Debug.Log($"[FirebaseManager] 사용자 상태 업데이트: {status}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 사용자 상태 업데이트 실패: {ex.Message}");
            }
        }
    }
}