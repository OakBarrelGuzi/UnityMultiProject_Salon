using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Firebase.Extensions;
using Salon.Interfaces;

namespace Salon.Firebase
{
    public class FirebaseManager : MonoBehaviour, IInitializable
    {
        private DatabaseReference dbReference;
        private FirebaseAuth auth;
        private FirebaseUser currentUser;

        public static FirebaseManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public bool IsInitialized { get; private set; }
        private TaskCompletionSource<bool> initializationComplete;

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

        void Start()
        {
            Initialize();
        }

        private void InitializeFirebase()
        {
            auth = FirebaseAuth.DefaultInstance;
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;

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
                }
                else
                {
                    Debug.Log("[Firebase] 사용자 로그아웃");
                    currentUser = null;
                }
            }
        }

        private async Task<string> GenerateUniqueTagAsync(string baseName)
        {
            try
            {
                await EnsureInitialized();
                if (!IsInitialized)
                {
                    throw new InvalidOperationException("Firebase가 초기화되지 않았습니다.");
                }

                if (string.IsNullOrEmpty(baseName))
                {
                    throw new ArgumentException("기본 이름이 비어있습니다.");
                }

                var snapshot = await dbReference.Child("Users")
                    .OrderByChild("DisplayName")
                    .StartAt($"{baseName}#")
                    .EndAt($"{baseName}#\uf8ff")
                    .GetValueAsync()
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            throw new Exception($"Firebase 쿼리 실패: {task.Exception?.InnerException?.Message}");
                        }
                        return task.Result;
                    });

                HashSet<int> usedTags = new HashSet<int>();

                if (snapshot != null && snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        var userData = JsonConvert.DeserializeObject<Database.UserData>(child.GetRawJsonValue());
                        if (userData?.DisplayName != null && userData.DisplayName.StartsWith($"{baseName}#"))
                        {
                            string tagStr = userData.DisplayName.Split('#')[1];
                            if (int.TryParse(tagStr, out int tagNum))
                            {
                                usedTags.Add(tagNum);
                            }
                        }
                    }
                }

                int newTag = 1;
                while (usedTags.Contains(newTag))
                {
                    newTag++;
                }

                return $"{baseName}#{newTag:D4}";
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
                    var userData = new Database.UserData(result.User.UserId, uniqueDisplayName, email);

                    // JsonSerializerSettings를 사용하여 null 값 처리
                    var serializerSettings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    };

                    string json = JsonConvert.SerializeObject(userData, serializerSettings);
                    Debug.Log($"[Firebase] 저장할 데이터: {json}");

                    // 데이터베이스에 저장
                    Debug.Log("[Firebase] 데이터베이스에 사용자 데이터 저장 시도...");
                    await dbReference.Child("Users").Child(result.User.UserId).SetRawJsonValueAsync(json);
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
                    // 데이터가 저장되었으므로 실패로 처리하지 않음
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
                await UpdateUserLastOnline(result.User.UserId);
                Debug.Log($"[Firebase] 로그인 성공: {result.User.Email}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 로그인 실패: {ex.Message}");
                return false;
            }
        }

        private async Task UpdateUserLastOnline(string userId)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    ["LastOnline"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await dbReference.Child("Users").Child(userId).UpdateChildrenAsync(updates);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 마지막 접속 시간 업데이트 실패: {ex.Message}");
            }
        }

        public async Task<Database.UserData> GetUserDataAsync(string userId)
        {
            try
            {
                var snapshot = await dbReference.Child("Users").Child(userId).GetValueAsync();
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
            auth.SignOut();
            LogManager.Instance.ShowLog("[Firebase] 로그아웃 완료");
        }

        public async void InitializeRooms()
        {
            try
            {
                Debug.Log("[Firebase] 방 초기화 시작");
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
                Debug.LogError($"[Firebase] 방 초기화 실패: {ex.Message}");
            }
        }

        private async Task CreateMissingRoomsAsync(HashSet<string> existingRooms)
        {
            Dictionary<string, Database.RoomData> rooms = new Dictionary<string, Database.RoomData>();

            for (int i = 1; i <= 10; i++)
            {
                string roomName = $"Room{i}";
                if (!existingRooms.Contains(roomName))
                {
                    rooms[roomName] = new Database.RoomData();
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
                    Debug.Log($"[Firebase] {rooms.Count}개의 방방 생성 완료");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Firebase] 방 생성 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.Log("[Firebase] 모든 방이 이미 존재함");
            }
        }

        // 디스플레이 네임 변경
        public async Task<bool> UpdateDisplayNameAsync(string newDisplayName)
        {
            if (currentUser == null) return false;

            try
            {
                // 새로운 고유 디스플레이 네임 생성
                string uniqueDisplayName = await GenerateUniqueTagAsync(newDisplayName);

                // Firebase Auth 프로필 업데이트
                var profile = new UserProfile { DisplayName = uniqueDisplayName };
                await currentUser.UpdateUserProfileAsync(profile);

                // 데이터베이스의 사용자 데이터 업데이트
                var updates = new Dictionary<string, object>
                {
                    ["DisplayName"] = uniqueDisplayName
                };
                await dbReference.Child("Users").Child(currentUser.UserId).UpdateChildrenAsync(updates);

                Debug.Log($"[Firebase] 디디스플레이 네임 변경 성공: {uniqueDisplayName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 디스플레이 네임 변경 실패: {ex.Message}");
                return false;
            }
        }

        // 현재 사용자의 디스플레이 네임 가져오기
        public string GetCurrentDisplayName()
        {
            return currentUser?.DisplayName ?? "Unknown";
        }
    }
}