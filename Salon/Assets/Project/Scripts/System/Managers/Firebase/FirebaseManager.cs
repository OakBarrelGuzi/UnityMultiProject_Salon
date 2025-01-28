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

        private string currentUserUID;
        public string CurrentUserUID => currentUserUID;
        public string CurrnetUserDisplayName { get; private set; }

        public bool IsInitialized { get; private set; }
        private TaskCompletionSource<bool> initializationComplete;

        private bool isInitializing = false;

        private bool isQuitting = false;

        void Start()
        {
            if (!IsInitialized && !isInitializing)
            {
                Initialize();
            }
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.Log("[FirebaseManager] 이미 초기화되었습니다.");
                initializationComplete?.SetResult(true);
                return;
            }

            if (isInitializing)
            {
                Debug.Log("[FirebaseManager] 이미 초기화 중입니다.");
                return;
            }

            isInitializing = true;

            try
            {
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebase();

                    if (auth.CurrentUser != null)
                    {
                        currentUser = auth.CurrentUser;
                        currentUserUID = currentUser.UserId;
                        CurrnetUserDisplayName = currentUser.DisplayName;
                        Debug.Log("[FirebaseManager] 사용자 이름: " + currentUserUID + "/ 이메일 : " + currentUser.Email);
                    }

                    IsInitialized = true;
                    initializationComplete?.SetResult(true);

                    if (currentUser != null)
                    {
                        await InitializeManagers();
                    }

                    Debug.Log("[FirebaseManager] Firebase 초기화 성공");
                }
                else
                {
                    Debug.LogError($"[FirebaseManager] Firebase 초기화 실패: {dependencyStatus}");
                    initializationComplete?.SetResult(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 초기화 실패: {ex.Message}");
                initializationComplete?.SetResult(false);
            }
            finally
            {
                isInitializing = false;
            }
        }

        public void Initialize()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeManagers()
        {
            try
            {
                if (ChannelManager.Instance != null)
                {
                    await ChannelManager.Instance.Initialize();
                    Debug.Log("[FirebaseManager] ChannelManager 레퍼런스 업데이트 완료");
                    Debug.Log("[FirebaseManager] 레퍼런스 업데이트 시점의 채널매니저 UID :" + ChannelManager.Instance.currentUserUID + " DisplayName : " + ChannelManager.Instance.currentUserDisplayName);
                }
                else
                {
                    Debug.LogError("[FirebaseManager] ChannelManager가 null입니다");
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

                if (GameRoomManager.Instance != null)
                {
                    await GameRoomManager.Instance.Initialize();
                    Debug.Log("[FirebaseManager] GameRoomManager 레퍼런스 업데이트 완료");
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

        }

        private async Task SetupDisconnectHandler(string userUID)
        {
            try
            {
                var userRef = dbReference.Child("Users").Child(userUID);
                await userRef.OnDisconnect().UpdateChildren(
                    new Dictionary<string, object> { { "Status", 3 } }
                );
                Debug.Log("[FirebaseManager] 연결 해제 핸들러 설정 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 연결 해제 핸들러 설정 실패: {ex.Message}");
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
                if (!string.IsNullOrEmpty(currentUserUID))
                {
                    _ = SetupDisconnectHandler(currentUserUID);
                }
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

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private async void OnDisable()
        {
            if (!Application.isPlaying || isQuitting) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            var userRef = dbReference.Child("Users").Child(currentUserUID);
            await userRef.Child("Status").SetValueAsync(3);
        }

        private async void OnApplicationPause(bool pause)
        {
            if (!Application.isPlaying || isQuitting) return;

            try
            {
                if (pause)
                {
                    Debug.Log("[FirebaseManager] 앱 일시정지, 연결 해제");
                    await UpdateUserStatus(UserStatus.Away);
                    FirebaseDatabase.DefaultInstance.GoOffline();
                }
                else
                {
                    Debug.Log("[FirebaseManager] 앱 재개, 연결 복구");
                    FirebaseDatabase.DefaultInstance.GoOnline();
                    await UpdateUserStatus(UserStatus.Online);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] OnApplicationPause 처리 실패: {ex.Message}");
            }
        }

        private async void OnDestroy()
        {
            if (!Application.isPlaying || isQuitting) return;

            try
            {
                isQuitting = true;
                Debug.Log("[FirebaseManager] OnDestroy 시작");
                await UpdateUserStatus(UserStatus.Offline);
                CleanupResources();
                Debug.Log("[FirebaseManager] OnDestroy 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] OnDestroy 중 오류 발생: {ex.Message}");
            }
        }

        private async void OnApplicationQuit()
        {
            if (!Application.isPlaying || isQuitting) return;

            try
            {
                isQuitting = true;
                Debug.Log("[FirebaseManager] OnApplicationQuit 시작");

                var updateTask = UpdateUserStatus(UserStatus.Offline);
                await Task.WhenAny(updateTask, Task.Delay(1000));

                CleanupResources();
                Debug.Log("[FirebaseManager] OnApplicationQuit 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] OnApplicationQuit 처리 실패: {ex.Message}");
            }
        }

        public async Task UpdateUserStatus(UserStatus status)
        {
            if (currentUser == null || string.IsNullOrEmpty(currentUserUID)) return;

            try
            {
                var userRef = dbReference?.Child("Users")?.Child(currentUserUID);
                if (userRef != null)
                {
                    var userData = new Dictionary<string, object>
                    {
                        ["Status"] = (int)status,
                        ["LastOnline"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    await userRef.UpdateChildrenAsync(userData);
                    Debug.Log($"[FirebaseManager] Status 상태 업데이트: {status}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] Status 상태 업데이트 실패: {ex.Message}");
            }
        }

        private void CleanupResources()
        {
            try
            {
                if (connectionRef != null)
                {
                    connectionRef.ValueChanged -= HandleConnectionChanged;
                    connectionRef = null;
                }

                SceneManager.sceneLoaded -= OnSceneLoaded;

                dbReference = null;
                auth = null;
                currentUser = null;
                IsInitialized = false;

                Debug.Log("[FirebaseManager] 핸들러 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 리소스 정리 중 오류 발생: {ex.Message}");
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

        public async Task<bool> SignUpWithEmailAsync(string email, string password)
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
                        DisplayName = uniqueDisplayName,
                        LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Status = UserStatus.Offline,
                        Friends = new Dictionary<string, bool>(),
                        GameStats = new Dictionary<GameType, UserStats>(),
                        Invites = new Dictionary<string, InviteData>(),
                        Gold = 50000,
                        Inventory = new UserInventory(),
                        ActivatedItems = new ActivatedItems(),
                    };

                    Debug.Log("[FirebaseManager] 데이터베이스에 사용자 데이터 저장 시도...");
                    string jsonData = JsonConvert.SerializeObject(userData);
                    await dbReference.Child("Users").Child(result.User.UserId).SetRawJsonValueAsync(jsonData);
                    Debug.Log($"[FirebaseManager] 사용자 데이터 저장 완료 (UID: {result.User.UserId})");
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
                Debug.Log("[FirebaseManager] 로그인 시도 시작");

                initializationComplete = new TaskCompletionSource<bool>();
                await InitializeAsync();
                if (!await initializationComplete.Task)
                {
                    Debug.LogError("[FirebaseManager] Firebase 초기화 실패");
                    return false;
                }

                var tempResult = await auth.SignInWithEmailAndPasswordAsync(email, password);
                string userUID = tempResult.User.UserId;
                Debug.Log($"[FirebaseManager] 임시 로그인 완료: {userUID}");

                var userSnapshot = await dbReference.Child("Users").Child(userUID).GetValueAsync();
                if (!userSnapshot.Exists)
                {
                    Debug.LogError("[FirebaseManager] 사용자 데이터가 존재하지 않습니다.");
                    auth.SignOut();
                    return false;
                }

                var userData = JsonConvert.DeserializeObject<Database.UserData>(userSnapshot.GetRawJsonValue());
                Debug.Log($"[FirebaseManager] 현재 사용자 상태: {userData.Status}, 마지막 접속: {DateTimeOffset.FromUnixTimeSeconds(userData.LastOnline).ToString()}");

                if (userData.Status == UserStatus.Online)
                {
                    long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    Debug.Log($"[FirebaseManager] 현재 시간: {DateTimeOffset.FromUnixTimeSeconds(currentTime).ToString()}, 시간 차이: {currentTime - userData.LastOnline}초");

                    if (currentTime - userData.LastOnline > 300) // 5분 = 300초
                    {
                        Debug.Log("[FirebaseManager] 마지막 접속 후 5분이 지나 강제 로그아웃 처리됨");
                        // 강제 로그아웃 처리를 위해 상태를 Offline으로 변경
                        await dbReference.Child("Users").Child(userUID).Child("Status").SetValueAsync((int)UserStatus.Offline);
                    }
                    else
                    {
                        LogManager.Instance.ShowLog("이미 다른 기기에서 로그인되어 있습니다.");
                        auth.SignOut();
                        return false;
                    }
                }

                currentUser = tempResult.User;
                currentUserUID = currentUser.UserId;
                CurrnetUserDisplayName = currentUser.DisplayName;

                await SetupDisconnectHandler(userUID);

                await InitializeManagers();

                userData.Status = UserStatus.Online;
                userData.LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonConvert.SerializeObject(userData);
                await dbReference.Child("Users").Child(userUID).SetRawJsonValueAsync(json);

                Debug.Log($"[FirebaseManager] 로그인 성공 - Email: {currentUser.Email}, UID: {currentUserUID}, DisplayName: {CurrnetUserDisplayName}");
                return true;
            }
            catch (FirebaseException ex)
            {
                HandleFirebaseError(ex);
                if (auth.CurrentUser != null)
                {
                    auth.SignOut();
                }
                return false;
            }
        }

        private void HandleFirebaseError(FirebaseException ex)
        {
            Debug.Log($"[FirebaseManager] 오류 코드: {ex.ErrorCode}");
            switch (ex.ErrorCode)
            {
                case 1:
                    LogManager.Instance.ShowLog("존재하지 않는 이메일이거나 비밀번호가 올바르지 않습니다");
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
        }

        private async Task UpdateUserLastOnline(string displayName)
        {
            try
            {
                var userData = await GetUserDataAsync(displayName) ?? new Database.UserData();
                userData.LastOnline = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                userData.Status = UserStatus.Online;

                string jsonData = JsonConvert.SerializeObject(userData);
                await dbReference.Child("Users").Child(displayName).SetRawJsonValueAsync(jsonData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 마지막 접속 시간 업데이트 실패: {ex.Message}");
            }
        }

        public async Task<Database.UserData> GetUserDataAsync(string UID)
        {
            if (string.IsNullOrEmpty(UID))
            {
                Debug.LogWarning("[FirebaseManager] displayName이 null이거나 비어있습니다.");
                return null;
            }

            try
            {
                var snapshot = await dbReference.Child("Users").Child(UID).GetValueAsync();
                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[FirebaseManager] 사용자 {UID}의 데이터가 없습니다.");
                    return null;
                }

                var rawJson = snapshot.GetRawJsonValue();
                if (string.IsNullOrEmpty(rawJson))
                {
                    Debug.LogWarning($"[FirebaseManager] 사용자 {UID}의 JSON 데이터가 비어있습니다.");
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

        private void RemoveAllListeners()
        {
            try
            {
                if (ChannelManager.Instance != null)
                    ChannelManager.Instance.RemoveAllListeners();
                if (ChatManager.Instance != null)
                    ChatManager.Instance.RemoveAllListeners();
                if (RoomManager.Instance != null)
                    RoomManager.Instance.RemoveAllListeners();
                if (FriendManager.Instance != null)
                    FriendManager.Instance.RemoveAllListeners();
                /*  if (GameRoomManager.Instance != null)
                     GameRoomManager.Instance.RemoveAllListeners();
  */
                Debug.Log("[FirebaseManager] 모든 매니저의 리스너 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseManager] 리스너 제거 중 오류: {ex.Message}");
            }
        }

        public async void SignOut()
        {
            try
            {
                if (currentUserUID != null)
                {
                    try
                    {
                        // 채널에서 나가기
                        if (ChannelManager.Instance != null)
                        {
                            await ChannelManager.Instance.LeaveChannel();
                        }

                        await dbReference.Child("Users").Child(currentUserUID).Child("Status").SetValueAsync(3);
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FirebaseManager] 상태 업데이트 실패: {ex.Message}");
                    }
                }

                RemoveAllListeners();

                if (SceneManager.GetActiveScene().name != "MainScene")
                {
                    Debug.Log("[Firebase Manager] MainScene으로 이동");
                    SceneManager.LoadScene("MainScene");
                }

                Debug.Log("[Firebase Manager] 로그아웃 처리 시작");
                auth.SignOut();
                currentUser = null;
                currentUserUID = null;
                CurrnetUserDisplayName = null;
                IsInitialized = false;

                Debug.Log("[Firebase Manager] 로그아웃 처리 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase Manager] 로그아웃 중 오류 발생: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                LogManager.Instance.ShowLog("로그아웃 중 오류가 발생했습니다.");
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[Firebase Manager] 씬 로드됨: {scene.name}");

            if (!isConnected || !IsInitialized || DbReference == null)
            {
                Debug.Log("[Firebase Manager] 재연결 시도");
                ReconnectFirebase();
            }
        }

        public async Task<string> GetUIDByDisplayName(string targetDisplayName)
        {
            try
            {
                Debug.Log($"[FirebaseManager] DisplayName으로 UID 검색 시작: {targetDisplayName}");
                string targetServerName = DisplayNameUtils.ToServerFormat(targetDisplayName);
                var usersSnapshot = await dbReference.Child("Users").OrderByChild("DisplayName").EqualTo(targetServerName).GetValueAsync();

                if (!usersSnapshot.Exists)
                {
                    Debug.LogWarning($"[FirebaseManager] DisplayName {targetServerName}을 가진 사용자를 찾을 수 없습니다.");
                    return null;
                }

                // 첫 번째(유일한) 결과의 키가 UID입니다
                foreach (var userSnapshot in usersSnapshot.Children)
                {
                    string uid = userSnapshot.Key;
                    Debug.Log($"[FirebaseManager] DisplayName {targetServerName}의 UID를 찾았습니다: {uid}");
                    return uid;
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] DisplayName으로 UID 검색 실패: {ex.Message}");
                return null;
            }
        }

        public async Task<String> GetDisplayNameByUID(string UID)
        {
            try
            {
                Debug.Log($"[FirebaseManager] UID로 DisplayName 검색 시작: {UID}");
                var Ref = dbReference.Child("Users").Child(UID).Child("DisplayName");

                var sanpShot = await Ref.GetValueAsync();
                if (!sanpShot.Exists)
                {
                    Debug.LogWarning($"[FirebaseManager] UID {UID}의 DisplayName이 존재하지 않습니다.");
                    return null;
                }

                var serverName = sanpShot.Value.ToString();
                string displayName = DisplayNameUtils.ToDisplayFormat(serverName);

                Debug.Log($"[FirebaseManager] UID {UID}의 DisplayName 찾음: {displayName} (서버명: {serverName})");
                return displayName;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] DisplayName 검색 실패: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateUsername(string newUsername)
        {
            if (currentUser == null || string.IsNullOrEmpty(currentUserUID))
            {
                Debug.LogError("[FirebaseManager] 사용자가 로그인되어 있지 않습니다.");
                return false;
            }

            try
            {
                string uniqueDisplayName = await GenerateUniqueTagAsync(newUsername);
                var profile = new UserProfile { DisplayName = uniqueDisplayName };
                await currentUser.UpdateUserProfileAsync(profile);

                var userRef = dbReference.Child("Users").Child(currentUserUID);
                await userRef.Child("DisplayName").SetValueAsync(uniqueDisplayName);

                CurrnetUserDisplayName = newUsername;

                Debug.Log($"[FirebaseManager] 사용자 이름이 성공적으로 업데이트되었습니다: {newUsername}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseManager] 사용자 이름 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        public async Task<UserData> GetUserData()
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserUID))
                {
                    Debug.LogError("현재 유저 UID가 없습니다.");
                    return null;
                }

                var snapshot = await dbReference.Child("Users").Child(currentUserUID).GetValueAsync();
                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    return JsonConvert.DeserializeObject<UserData>(json);
                }
                return new UserData();
            }
            catch (Exception e)
            {
                Debug.LogError($"유저 데이터 로드 실패: {e.Message}");
                return null;
            }
        }

        public async Task UpdateUserData(UserData userData)
        {
            try
            {
                if (string.IsNullOrEmpty(currentUserUID))
                {
                    Debug.LogError("현재 유저 UID가 없습니다.");
                    return;
                }

                // 기존 데이터를 가져옵니다
                var snapshot = await dbReference.Child("Users").Child(currentUserUID).GetValueAsync();
                if (snapshot.Exists)
                {
                    var existingData = JsonConvert.DeserializeObject<UserData>(snapshot.GetRawJsonValue());
                    // DisplayName을 기존 값으로 유지
                    userData.DisplayName = existingData.DisplayName;
                }
                else
                {
                    // 기존 데이터가 없는 경우 현재 DisplayName 사용
                    userData.DisplayName = CurrnetUserDisplayName;
                }

                string json = JsonConvert.SerializeObject(userData);
                await dbReference.Child("Users").Child(currentUserUID).SetRawJsonValueAsync(json);
                Debug.Log("유저 데이터 업데이트 완료");
            }
            catch (Exception e)
            {
                Debug.LogError($"유저 데이터 업데이트 실패: {e.Message}");
                throw;
            }
        }
    }
}