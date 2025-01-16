using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Salon.Firebase.Database;
using Salon.System;
using UnityEngine.Events;
using Salon.UI;

namespace Salon.Firebase
{
    public class FriendManager : Singleton<FriendManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference friendRequestsRef;

        public UnityEvent<string> OnFriendRequestReceived = new UnityEvent<string>();

        public bool isInitialized = false;

        public async Task Initialize()
        {
            try
            {
                Debug.Log("[FriendManager] 초기화 시작");
                await ResetAndSetupReferences();
                isInitialized = true;
                Debug.Log("[FriendManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초기화 실패: {ex.Message}");
                isInitialized = false;
            }
        }

        public async Task ResetAndSetupReferences()
        {
            try
            {
                await Task.Delay(1000);
                // 1. 기존 리스너 제거 및 참조 초기화
                StopListening();
                friendRequestsRef = null;

                // 2. 새로운 참조 설정
                dbReference = FirebaseManager.Instance.DbReference;

                if (string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserName))
                {
                    Debug.LogWarning("[FriendManager] 현재 로그인된 사용자가 없습니다.");
                    return;
                }

                // 3. 새로운 FriendRequests 참조 설정
                string currentUserPath = $"Users/{FirebaseManager.Instance.CurrentUserName}";
                friendRequestsRef = dbReference.Child(currentUserPath).Child("FriendRequests");
                Debug.Log($"[FriendManager] 친구 요청 참조 설정: {friendRequestsRef.Reference.ToString()}");

                // 4. 리스너 시작
                StartListening();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 참조 재설정 실패: {ex.Message}");
                throw;
            }
        }

        private void StartListening()
        {
            if (friendRequestsRef != null)
            {
                friendRequestsRef.ChildAdded += OnFriendRequestAdded;
                Debug.Log("[FriendManager] 친구 요청 리스닝 시작");
            }
        }

        private void StopListening()
        {
            if (friendRequestsRef != null)
            {
                friendRequestsRef.ChildAdded -= OnFriendRequestAdded;
                Debug.Log("[FriendManager] 친구 요청 리스닝 중지");
            }
        }

        private void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
        {
            try
            {
                Debug.Log($"[FriendManager] 새 친구 요청 감지: {args.Snapshot.Key}");

                if (!args.Snapshot.Exists)
                {
                    Debug.LogWarning("[FriendManager] 존재하지 않는 스냅샷");
                    return;
                }

                var requestData = JsonConvert.DeserializeObject<FriendRequestData>(args.Snapshot.GetRawJsonValue());
                if (requestData?.status != "pending")
                {
                    Debug.Log("[FriendManager] pending 상태가 아닌 요청");
                    return;
                }

                if (requestData.sender == FirebaseManager.Instance.CurrentUserName)
                {
                    Debug.Log("[FriendManager] 자신이 보낸 요청이므로 무시");
                    return;
                }

                string senderServerName = args.Snapshot.Key;
                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);

                Debug.Log($"[FriendManager] 친구 요청 표시 - 발신자: {senderDisplayName}");
                OnFriendRequestReceived?.Invoke(senderDisplayName);
                ShowFriendRequestPopUp(senderDisplayName, senderServerName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 처리 중 오류: {ex.Message}");
            }
        }

        public async Task SendFriendRequest(string targetDisplayName)
        {
            try
            {
                string targetServerName = DisplayNameUtils.ToServerFormat(targetDisplayName);
                string currentUserName = FirebaseManager.Instance.CurrentUserName;

                if (targetServerName == currentUserName)
                {
                    LogManager.Instance.ShowLog("자신에게 친구 요청을 보낼 수 없습니다.");
                    return;
                }

                var requestData = new Dictionary<string, object>
                {
                    ["sender"] = currentUserName,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["status"] = "pending"
                };

                await dbReference.Child("Users").Child(targetServerName).Child("FriendRequests").Child(currentUserName)
                    .UpdateChildrenAsync(requestData);

                LogManager.Instance.ShowLog($"{targetDisplayName}님에게 친구 요청을 보냈습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 전송 실패: {ex.Message}");
                LogManager.Instance.ShowLog("친구 요청 전송에 실패했습니다.");
            }
        }

        public async Task AcceptFriendRequest(string senderServerName)
        {
            try
            {
                string currentUserName = FirebaseManager.Instance.CurrentUserName;

                // 1. 친구 목록에 추가
                var updates = new Dictionary<string, object>
                {
                    [$"Users/{currentUserName}/Friends/{senderServerName}"] = true,
                    [$"Users/{senderServerName}/Friends/{currentUserName}"] = true
                };
                await dbReference.UpdateChildrenAsync(updates);

                // 2. 수락 알림 전송
                var notification = new Dictionary<string, object>
                {
                    ["type"] = "friend_request_accepted",
                    ["accepter"] = currentUserName,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await dbReference.Child("Users").Child(senderServerName).Child("Notifications").Push()
                    .UpdateChildrenAsync(notification);

                // 3. 요청 삭제
                await friendRequestsRef.Child(senderServerName).RemoveValueAsync();

                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);
                LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 수락했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 수락 실패: {ex.Message}");
                LogManager.Instance.ShowLog("친구 요청 수락에 실패했습니다.");
            }
        }

        public async Task DeclineFriendRequest(string senderServerName)
        {
            try
            {
                string currentUserName = FirebaseManager.Instance.CurrentUserName;

                // 1. 거절 알림 전송
                var notification = new Dictionary<string, object>
                {
                    ["type"] = "friend_request_declined",
                    ["decliner"] = currentUserName,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await dbReference.Child("Users").Child(senderServerName).Child("Notifications").Push()
                    .UpdateChildrenAsync(notification);

                // 2. 요청 삭제
                await friendRequestsRef.Child(senderServerName).RemoveValueAsync();

                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);
                LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 거절했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 거절 실패: {ex.Message}");
                LogManager.Instance.ShowLog("친구 요청 거절에 실패했습니다.");
            }
        }

        private void ShowFriendRequestPopUp(string senderDisplayName, string senderServerName)
        {
            PopUpManager.Instance.ShowPopUp(
                $"{senderDisplayName}님이 친구 요청을 보냈습니다.",
                async () => await AcceptFriendRequest(senderServerName),
                async () => await DeclineFriendRequest(senderServerName)
            );
        }

        public async Task<Dictionary<string, UserData>> GetFriendsList()
        {
            try
            {
                var snapshot = await dbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserName)
                    .Child("Friends").GetValueAsync();

                var friendsList = new Dictionary<string, UserData>();
                if (snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        var friendData = await FirebaseManager.Instance.GetUserDataAsync(child.Key);
                        if (friendData != null)
                        {
                            string displayName = DisplayNameUtils.ToDisplayFormat(child.Key);
                            friendsList[displayName] = friendData;
                        }
                    }
                }
                return friendsList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 목록 조회 실패: {ex.Message}");
                return new Dictionary<string, UserData>();
            }
        }

        public void Cleanup()
        {
            try
            {
                Debug.Log("[FriendManager] 정리 작업 시작");
                StopListening();
                friendRequestsRef = null;
                dbReference = null;
                isInitialized = false;
                Debug.Log("[FriendManager] 정리 작업 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 정리 작업 중 오류: {ex.Message}");
            }
        }
    }
}