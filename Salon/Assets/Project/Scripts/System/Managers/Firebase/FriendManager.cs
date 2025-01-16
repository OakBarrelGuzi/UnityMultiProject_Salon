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
using System.Drawing.Text;

namespace Salon.Firebase
{
    public class FriendManager : Singleton<FriendManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference friendRequestsRef;
        private DatabaseReference invitesRef;
        private string currentUserName;

        public UnityEvent<string> OnFriendRequestReceived = new UnityEvent<string>();

        public bool isInitialized = false;

        public async Task Initialize()
        {
            try
            {
                currentUserName = FirebaseManager.Instance.CurrentUserName;
                Debug.Log($"[FriendManager] 초기화 시작");
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

                if (string.IsNullOrEmpty(currentUserName))
                {
                    Debug.LogWarning("[FriendManager] 현재 로그인된 사용자가 없습니다.");
                    return;
                }

                // 3. 새로운 FriendRequests 참조 설정
                string currentUserPath = $"Users/{currentUserName}";
                friendRequestsRef = dbReference.Child(currentUserPath).Child("FriendRequests");

                invitesRef = dbReference.Child(currentUserPath).Child("Invites");

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
                Debug.Log("[FriendManager] 친구요청 리스닝 시작");
                invitesRef.ChildAdded += OnInvitesAdded;
                Debug.Log("[FriendManager] 초대 리스닝 시작");
            }
        }

        private void StopListening()
        {
            if (friendRequestsRef != null)
            {
                friendRequestsRef.ChildAdded -= OnFriendRequestAdded;
                Debug.Log("[FriendManager] 친구요청 리스닝 중지");
            }
            if (invitesRef != null)
            {
                invitesRef.ChildAdded -= OnInvitesAdded;
                Debug.Log("[FriendManager] 초대 리스닝 중지");
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

        #region Channel Invite

        public async Task SendInvite(string targetDisplayName)
        {
            if (!await ValdiateInivite(targetDisplayName))
            {
                return;
            }
            string targetServerName = DisplayNameUtils.ToServerFormat(targetDisplayName);
            string currentUserName = FirebaseManager.Instance.CurrentUserName;
            var targetRef = dbReference.Child("Users").Child(targetServerName).Child("Invites").Child(currentUserName);

            InviteData inviteData = new InviteData();
            inviteData.ChannelName = ChannelManager.Instance.CurrentChannel;
            inviteData.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            inviteData.Status = InviteStatus.Pending;

            string jsonData = JsonConvert.SerializeObject(inviteData);
            await targetRef.SetRawJsonValueAsync(jsonData);
        }

        public void OnInvitesAdded(object sender, ChildChangedEventArgs args)
        {
            Debug.Log($"[FriendManager] 초대 감지: {args.Snapshot.Key}");
            string inviteData = args.Snapshot.GetRawJsonValue();
            InviteData invite = JsonConvert.DeserializeObject<InviteData>(inviteData);
            if (invite.Status == InviteStatus.Pending)
            {
                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(args.Snapshot.Key);
                PopUpManager.Instance.ShowPopUp(
                    $"{senderDisplayName}님이 채널 초대를 보냈습니다.",
                    async () => await AcceptInvite(args.Snapshot.Key, invite.ChannelName),
                    async () => await DeclineInvite(args.Snapshot.Key)
                );
            }
        }

        private async Task<bool> ValdiateInivite(string targetUserName)
        {
            try
            {
                Debug.Log($"[FriendManager] 초대 검증 시작: {targetUserName}");

                // 1. 채널 데이터 확인
                var targetChannelRef = dbReference.Child("Channels").Child(ChannelManager.Instance.CurrentChannel);
                var playersRef = targetChannelRef.Child("Players");

                var playersSnapshot = await playersRef.GetValueAsync();
                if (!playersSnapshot.Exists)
                {
                    Debug.Log("[FriendManager] Players 데이터가 없습니다.");
                    return true;
                }

                var players = JsonConvert.DeserializeObject<Dictionary<string, GamePlayerData>>(playersSnapshot.GetRawJsonValue());
                if (players != null && players.ContainsKey(targetUserName))
                {
                    LogManager.Instance.ShowLog($"{targetUserName}님이 이미 채널에 존재합니다.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 검증 실패: {ex.Message}");
                Debug.LogError($"[FriendManager] 스택 트레이스: {ex.StackTrace}");
                return false;
            }
        }

        private async Task AcceptInvite(string senderServerName, string targetServer)
        {
            var targetInviteRef = dbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserName).Child("Invites").Child(senderServerName);
            await targetInviteRef.RemoveValueAsync();
            await ChannelManager.Instance.JoinChannel(targetServer);
        }

        private async Task DeclineInvite(string senderServerName)
        {
            await invitesRef.Child(senderServerName).RemoveValueAsync();
        }

        #endregion

        private void OnApplicationQuit()
        {
            StopListening();
        }
    }
}