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

namespace Salon.Firebase
{
    public class FirebaseManager : MonoBehaviour
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

        void Start()
        {
            // Firebase SDK 초기화
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    Debug.Log("[Firebase] 초기화 성공");
                    InitializeFirebase();
                }
                else
                {
                    Debug.LogError($"[Firebase] 초기화 실패: {dependencyStatus}");
                }
            });
        }

        private void InitializeFirebase()
        {
            // Firebase 인증 및 데이터베이스 초기화
            auth = FirebaseAuth.DefaultInstance;
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;

            // 인증 상태 변경 이벤트 구독
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

        // 이메일/비밀번호로 회원가입
        public async Task<bool> RegisterWithEmailAsync(string email, string password)
        {
            try
            {
                var result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
                Debug.Log($"[Firebase] 회원가입 성공: {result.User.Email}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 회원가입 실패: {ex.Message}");
                return false;
            }
        }

        // 이메일/비밀번호로 로그인
        public async Task<bool> SignInWithEmailAsync(string email, string password)
        {
            try
            {
                var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
                Debug.Log($"[Firebase] 로그인 성공: {result.User.Email}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Firebase] 로그인 실패: {ex.Message}");
                return false;
            }
        }

        // 로그아웃
        public void SignOut()
        {
            auth.SignOut();
            Debug.Log("[Firebase] 로그아웃 완료");
        }

        // 방 존재 여부 확인 및 생성
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
                    Debug.Log($"[Firebase] {rooms.Count}개의 방 생성 완료");
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
    }
}