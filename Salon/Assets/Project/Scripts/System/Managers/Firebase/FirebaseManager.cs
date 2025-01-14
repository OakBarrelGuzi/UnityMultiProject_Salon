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

        void Start()
        {
            Initialize();
        }

        public async void Initialize()
        {
            try
            {
                initializationComplete = new TaskCompletionSource<bool>();
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebase();
                    IsInitialized = true;
                    initializationComplete.SetResult(true);
                    Debug.Log("[Firebase] 초기화 성공");
                }
                else
                {
                    Debug.LogError($"[Firebase] 초기화 실패: {dependencyStatus}");
                    initializationComplete.SetResult(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 초기화 실패: {ex.Message}");
                initializationComplete.SetResult(false);
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

        private void AuthStateChanged(object sender, EventArgs e)
        {
            if (auth.CurrentUser != currentUser)
            {
                bool signedIn = (auth.CurrentUser != null);
                if (signedIn)
                {
                    Debug.Log($"[Firebase] 사용자 로그인: {auth.CurrentUser.Email}");
                    currentUser = auth.CurrentUser;
                    currentUserName = currentUser.DisplayName;
                    Debug.Log($"[Firebase] 현재 사용자 이름: {currentUserName}");

                    if (ChannelManager.Instance != null)
                    {
                        ChannelManager.Instance.SetCurrentUserName(currentUserName);
                    }
                }
                else
                {
                    Debug.Log("[Firebase] 사용자 로그아웃");
                    currentUser = null;
                    currentUserName = null;

                    if (ChannelManager.Instance != null)
                    {
                        ChannelManager.Instance.SetCurrentUserName(null);
                    }
                }
            }
        }

        private void HandleConnectionChanged(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"[Firebase] 연결 상태 확인 오류: {args.DatabaseError.Message}");
                return;
            }

            isConnected = (bool)args.Snapshot.Value;
            if (isConnected)
            {
                Debug.Log("[Firebase] 연결됨");
            }
            else
            {
                Debug.LogWarning("[Firebase] 연결 끊김, 재연결 시도...");
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
                    Debug.Log("[Firebase] 재초기화 시도");
                    Initialize();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 재연결 실패: {ex.Message}");
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                Debug.Log("[Firebase] 앱 일시정지, 연결 해제");
                FirebaseDatabase.DefaultInstance.GoOffline();
            }
            else
            {
                Debug.Log("[Firebase] 앱 재개, 연결 복구");
                FirebaseDatabase.DefaultInstance.GoOnline();
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;

            try
            {
                Debug.Log("[Firebase Manager] OnDestroy 시작");

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

                Debug.Log("[Firebase Manager] 핸들러 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase Manager] OnDestroy 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task<string> GenerateUniqueTagAsync(string baseName)
        {
            try
            {
                Debug.Log($"[Firebase] 고유 태그 생성 시작 - 기본 이름: {baseName}");
                await EnsureInitialized();
                if (!IsInitialized)
                {
                    Debug.LogError("[Firebase] Firebase가 초기화되지 않았습니다.");
                    throw new InvalidOperationException("Firebase가 초기화되지 않았습니다.");
                }

                if (string.IsNullOrEmpty(baseName))
                {
                    Debug.LogError("[Firebase] 기본 이름이 비어있습니다.");
                    throw new ArgumentException("기본 이름이 비어있습니다.");
                }

                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                const int tagLength = 4;

                int maxAttempts = 100; // 무한 루프 방지를 위한 최대 시도 횟수
                int attempts = 0;

                while (attempts < maxAttempts)
                {
                    // 4글자 랜덤 태그 생성
                    char[] tagArray = new char[tagLength];
                    for (int i = 0; i < tagLength; i++)
                    {
                        tagArray[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
                    }
                    string randomTag = new string(tagArray);
                    string uniqueTag = $"{baseName}_{randomTag}";

                    Debug.Log($"[Firebase] 태그 생성 시도 {attempts + 1}: {uniqueTag}");

                    // 태그 중복 확인
                    var snapshot = await dbReference.Child("Users").Child(uniqueTag).GetValueAsync();
                    if (!snapshot.Exists)
                    {
                        Debug.Log($"[Firebase] 고유한 태그 생성 성공: {uniqueTag} (시도 횟수: {attempts + 1})");
                        return uniqueTag;
                    }

                    Debug.Log($"[Firebase] 태그 {uniqueTag}가 이미 존재함, 재시도...");
                    attempts++;
                }

                throw new Exception($"[Firebase] {maxAttempts}번의 시도 후에도 고유한 태그를 생성하지 못했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 태그 생성 실패: {ex.Message}\n{ex.StackTrace}");
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
                    Debug.LogError("[Firebase] Firebase가 초기화되지 않았습니다.");
                    return false;
                }

                Debug.Log($"[Firebase] 회원가입 시도: {email}");

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    Debug.LogError("[Firebase] 이메일 또는 비밀번호가 비어있습니다.");
                    return false;
                }

                if (password.Length < 6)
                {
                    Debug.LogError("[Firebase] 비밀번호는 최소 6자 이상이어야 합니다.");
                    return false;
                }

                Debug.Log("[Firebase] 사용자 생성 시도...");
                var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);

                if (result == null || result.User == null)
                {
                    Debug.LogError("[Firebase] 사용자 생성 결과가 null입니다.");
                    return false;
                }

                Debug.Log("[Firebase] 고유 태그 생성 시도...");
                string uniqueDisplayName = await GenerateUniqueTagAsync("user");
                Debug.Log($"[Firebase] 생성된 태그: {uniqueDisplayName}");

                Debug.Log("[Firebase] 프로필 업데이트 시도...");
                var profile = new UserProfile { DisplayName = uniqueDisplayName };
                await result.User.UpdateUserProfileAsync(profile);
                Debug.Log("[Firebase] 프로필 업데이트 완료");

                try
                {
                    Debug.Log("[Firebase] 사용자 데이터 생성 시도...");

                    // 사용자 데이터를 Dictionary로 직접 생성
                    var userData = new Dictionary<string, object>
                    {
                        ["LastOnline"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ["Friends"] = new Dictionary<string, bool>(),
                        ["GameStats"] = new Dictionary<string, object>()
                    };

                    // 데이터베이스에 저장 (DisplayName을 키로 사용)
                    Debug.Log("[Firebase] 데이터베이스에 사용자 데이터 저장 시도...");
                    await dbReference.Child("Users").Child(uniqueDisplayName).UpdateChildrenAsync(userData);
                    Debug.Log("[Firebase] 사용자 데이터 저장 완료");

                    Debug.Log("[Firebase] 모든 데이터베이스 작업 완료");
                    Debug.Log($"[Firebase] 회원가입 성공: {result.User.Email} ({uniqueDisplayName})");
                    return true;
                }
                catch (Exception dbEx)
                {
                    Debug.LogError($"[Firebase] 데이터베이스 작업 실패 상세: {dbEx.GetType().Name} - {dbEx.Message}");
                    if (dbEx.InnerException != null)
                    {
                        Debug.LogError($"[Firebase] 내부 예외: {dbEx.InnerException.Message}");
                        Debug.LogError($"[Firebase] 내부 예외 스택 트레이스: {dbEx.InnerException.StackTrace}");
                    }
                    Debug.LogError($"[Firebase] 스택 트레이스: {dbEx.StackTrace}");
                    return true;
                }
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[Firebase] Firebase 예외: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 일반 예외: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
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
                Debug.Log($"[Firebase] 로그인 성공: {result.User.Email} (DisplayName: {currentUserName})");

                // ChannelManager에 현재 사용자 이름 설정
                if (ChannelManager.Instance != null)
                {
                    ChannelManager.Instance.SetCurrentUserName(currentUserName);
                }
                else
                {
                    Debug.LogError("[Firebase] ChannelManager가 null입니다");
                }

                LogManager.Instance.ShowLog("로그인 성공!");
                await ChannelManager.Instance.ExistRooms();
                return true;
            }
            catch (FirebaseException ex)
            {
                Debug.LogError($"[Firebase] 로그인 실패: {ex.Message}");
                Debug.Log($"[Firebase] 오류 코드: {ex.ErrorCode}");
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
                    case -2:  // 내부 오류 코드
                        LogManager.Instance.ShowLog("서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
                        break;
                    default:
                        LogManager.Instance.ShowLog("로그인에 실패했습니다. 다시 시도해주세요.");
                        break;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 로그인 실패: {ex.Message}");
                LogManager.Instance.ShowLog("로그인 중 오류가 발생했습니다.");
                return false;
            }
        }

        private async Task UpdateUserLastOnline(string displayName)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    ["LastOnline"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await dbReference.Child("Users").Child(displayName).UpdateChildrenAsync(updates);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 마지막 접속 시간 업데이트 실패: {ex.Message}");
            }
        }

        public async Task<Database.UserData> GetUserDataAsync(string displayName)
        {
            try
            {
                var snapshot = await dbReference.Child("Users").Child(displayName).GetValueAsync();
                if (snapshot.Exists)
                {
                    return JsonConvert.DeserializeObject<Database.UserData>(snapshot.GetRawJsonValue());
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 사용자 정보 조회 실패: {ex.Message}");
                return null;
            }
        }

        public async Task<Dictionary<string, Database.UserData>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                var result = new Dictionary<string, Database.UserData>();
                var snapshot = await dbReference.Child("Users")
                    .OrderByChild("DisplayName")
                    .StartAt(searchTerm)
                    .EndAt(searchTerm + "\uf8ff")
                    .GetValueAsync();

                foreach (var child in snapshot.Children)
                {
                    var userData = JsonConvert.DeserializeObject<Database.UserData>(child.GetRawJsonValue());
                    result[child.Key] = userData;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 사용자 검색 실패: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> AddFriendAsync(string friendUserId)
        {
            if (currentUser == null) return false;

            try
            {
                var updates = new Dictionary<string, object>
                {
                    [$"Users/{currentUser.UserId}/Friends/{friendUserId}"] = true,
                    [$"Users/{friendUserId}/Friends/{currentUser.UserId}"] = true
                };

                await dbReference.UpdateChildrenAsync(updates);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 친구 추가 실패: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, Database.UserData>> GetFriendsAsync()
        {
            if (currentUser == null) return null;

            try
            {
                var result = new Dictionary<string, Database.UserData>();
                var snapshot = await dbReference.Child("Users").Child(currentUser.UserId).Child("Friends").GetValueAsync();

                foreach (var child in snapshot.Children)
                {
                    var friendData = await GetUserDataAsync(child.Key);
                    if (friendData != null)
                    {
                        result[child.Key] = friendData;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 친구 목록 조회 실패: {ex.Message}");
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
    }
}