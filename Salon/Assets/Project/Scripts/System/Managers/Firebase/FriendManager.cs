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
using UnityEngine.SceneManagement;

namespace Salon.Firebase
{
    public class FriendManager : Singleton<FriendManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference friendRequestsRef;
        private DatabaseReference invitesRef;

        public UnityEvent<string> OnFriendRequestReceived = new UnityEvent<string>();

        public bool isInitialized = false;

        public async Task Initialize()
        {
            try
            {
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

                if (string.IsNullOrEmpty(FirebaseManager.Instance.CurrentUserUID))
                {
                    Debug.LogWarning("[FriendManager] 현재 로그인된 사용자가 없습니다.");
                    return;
                }

                // 3. 새로운 FriendRequests 참조 설정
                string currentUserPath = $"Users/{FirebaseManager.Instance.CurrentUserUID}";
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

            GetPendingFriendsRequest();
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

        private async void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
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

                if (requestData.sender == FirebaseManager.Instance.CurrentUserUID)
                {
                    Debug.Log("[FriendManager] 자신이 보낸 요청이므로 무시");
                    return;
                }

                string senderUID = args.Snapshot.Key;
                var senderDisplayNameRef = dbReference.Child("Users").Child(senderUID).Child("DisplayName");

                var displayNameSnapshot = await senderDisplayNameRef.GetValueAsync();

                string senderServerName = displayNameSnapshot.Value.ToString();
                string currentUserName = FirebaseManager.Instance.CurrentUserUID;

                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);

                Debug.Log($"[FriendManager] 친구 요청 표시 - 발신자: {senderDisplayName}");
                OnFriendRequestReceived?.Invoke(senderDisplayName);
                ShowFriendRequestPopUp(senderDisplayName, senderUID);
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
                print($"[FriendManager] 친구요청 보내기 시작");
                string targetServerName = DisplayNameUtils.ToServerFormat(targetDisplayName);
                var targetUID = await FirebaseManager.Instance.GetUIDByDisplayName(targetServerName);
                print($"[FriendManager] targetUID : {targetUID}");
                string currentUserUID = FirebaseManager.Instance.CurrentUserUID;
                print($"[FriendManager] currentUserUID : {currentUserUID}");

                if (targetUID == currentUserUID)
                {
                    LogManager.Instance.ShowLog("자신에게 친구 요청을 보낼 수 없습니다.");
                    return;
                }

                var requestData = new Dictionary<string, object>
                {
                    ["sender"] = currentUserUID,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["status"] = "pending"
                };

                await dbReference.Child("Users").Child(targetUID).Child("FriendRequests").Child(currentUserUID)
                    .UpdateChildrenAsync(requestData);

                LogManager.Instance.ShowLog($"{targetDisplayName}님에게 친구 요청을 보냈습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 전송 실패: {ex.Message}");
                LogManager.Instance.ShowLog("친구 요청 전송에 실패했습니다.");
            }
        }

        public async Task AcceptFriendRequest(string senderUID)
        {
            try
            {
                string currentUserUID = FirebaseManager.Instance.CurrentUserUID;

                // 1. 친구 목록에 추가
                var updates = new Dictionary<string, object>
                {
                    [$"Users/{currentUserUID}/Friends/{senderUID}"] = true,
                    [$"Users/{senderUID}/Friends/{currentUserUID}"] = true
                };
                await dbReference.UpdateChildrenAsync(updates);

                // 2. 수락 알림 전송
                var notification = new Dictionary<string, object>
                {
                    ["type"] = "friend_request_accepted",
                    ["accepter"] = currentUserUID,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await dbReference.Child("Users").Child(senderUID).Child("Notifications").Push()
                    .UpdateChildrenAsync(notification);

                // 3. 요청 삭제
                await friendRequestsRef.Child(senderUID).RemoveValueAsync();

                var senderDisplayNameRef = dbReference.Child("Users").Child(senderUID).Child("DisplayName");

                var displayNameSnapshot = await senderDisplayNameRef.GetValueAsync();

                string senderServerName = displayNameSnapshot.Value.ToString();

                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);

                LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 수락했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 수락 실패: {ex.Message}");
                LogManager.Instance.ShowLog("친구 요청 수락에 실패했습니다.");
            }
        }

        public async Task DeclineFriendRequest(string senderUID)
        {
            try
            {
                string currentUserName = FirebaseManager.Instance.CurrentUserUID;

                string senderDisplayName = await FirebaseManager.Instance.GetDisplayNameByUID(senderUID);

                // 1. 거절 알림 전송

                // 2. 요청 삭제
                await friendRequestsRef.Child(senderUID).RemoveValueAsync();

                LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 거절했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 거절 실패: {ex.Message}");
                LogManager.Instance.ShowLog("친구 요청 거절에 실패했습니다.");
            }
        }

        private async void GetPendingFriendsRequest()
        {
            var snapshot = await friendRequestsRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.Log("[FriendManager] 친구 요청 없음");
                return;
            }
            else
            {

                foreach (var child in snapshot.Children)
                {
                    string senderUID = child.Key;
                    var requestData = JsonConvert.DeserializeObject<FriendRequestData>(child.GetRawJsonValue());
                    if (requestData.status == "pending")
                    {
                        Debug.Log($"[FriendManager] 친구 요청 발신자: {senderUID}");
                        OnFriendRequestReceived?.Invoke(senderUID);
                    }

                }
            }
        }

        private void ShowFriendRequestPopUp(string senderDisplayName, string senderUID)
        {
            PopUpManager.Instance.ShowPopUp(
                $"{senderDisplayName}님이 친구 요청을 보냈습니다.",
                async () => await AcceptFriendRequest(senderUID),
                async () => await DeclineFriendRequest(senderUID)
            );
        }

        public async Task<Dictionary<string, UserData>> GetFriendsList()
        {
            try
            {
                Debug.Log("[FriendManager] 친구 목록 조회 시작");
                var snapshot = await dbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID)
                    .Child("Friends").GetValueAsync();

                var friendsList = new Dictionary<string, UserData>();
                if (snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        string friendUID = child.Key;
                        var friendData = await FirebaseManager.Instance.GetUserDataAsync(friendUID);
                        if (friendData != null)
                        {
                            string displayName = await FirebaseManager.Instance.GetDisplayNameByUID(friendUID);
                            if (!string.IsNullOrEmpty(displayName))
                            {
                                Debug.Log($"[FriendManager] 친구 정보 로드 - UID: {friendUID}, DisplayName: {displayName}");
                                friendsList[displayName] = friendData;
                            }
                        }
                    }
                }
                Debug.Log($"[FriendManager] 친구 목록 조회 완료 - 총 {friendsList.Count}명");
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
            string targetUID = await FirebaseManager.Instance.GetUIDByDisplayName(targetDisplayName);
            if (!await ValdiateInivite(targetDisplayName))
            {
                return;
            }
            string currentUserUID = FirebaseManager.Instance.CurrentUserUID;
            var targetRef = dbReference.Child("Users").Child(targetUID).Child("Invites").Child(currentUserUID);

            InviteData inviteData = new InviteData();
            inviteData.ChannelName = ChannelManager.Instance.CurrentChannel;
            inviteData.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            inviteData.Status = InviteStatus.Pending;

            string jsonData = JsonConvert.SerializeObject(inviteData);
            await targetRef.SetRawJsonValueAsync(jsonData);
        }

        public async void OnInvitesAdded(object sender, ChildChangedEventArgs args)
        {
            Debug.Log($"[FriendManager] 초대 감지: {args.Snapshot.Key}");
            string inviteData = args.Snapshot.GetRawJsonValue();
            InviteData invite = JsonConvert.DeserializeObject<InviteData>(inviteData);
            if (invite.Status == InviteStatus.Pending)
            {
                string senderDisplayName = await FirebaseManager.Instance.GetDisplayNameByUID(args.Snapshot.Key);

                PopUpManager.Instance.ShowPopUp(
                    $"{senderDisplayName}님이 채널 초대를 보냈습니다.",
                    async () => await AcceptInvite(args.Snapshot.Key, invite.ChannelName),
                    async () => await DeclineInvite(args.Snapshot.Key)
                );
            }
        }

        private async Task<bool> ValdiateInivite(string targetDisplayName)
        {
            try
            {
                Debug.Log($"[FriendManager] 초대 검증 시작: {targetDisplayName}");

                var targetChannelRef = dbReference.Child("Channels").Child(ChannelManager.Instance.CurrentChannel);
                var playersRef = targetChannelRef.Child("Players");

                var playersSnapshot = await playersRef.GetValueAsync();
                if (!playersSnapshot.Exists)
                {
                    Debug.Log("[FriendManager] Players 데이터가 없습니다.");
                    return true;
                }

                var players = JsonConvert.DeserializeObject<Dictionary<string, GamePlayerData>>(playersSnapshot.GetRawJsonValue());
                if (players != null && players.ContainsKey(targetDisplayName))
                {
                    LogManager.Instance.ShowLog($"{targetDisplayName}님이 이미 채널에 존재합니다.");
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

        private async Task AcceptInvite(string senderUID, string targetServer)
        {
            try
            {
                var targetInviteRef = dbReference.Child("Users").Child(FirebaseManager.Instance.CurrentUserUID).Child("Invites").Child(senderUID);
                var notificaiton = new Dictionary<string, object>
                {
                    ["type"] = "invite_request_accepted",
                    ["accepter"] = FirebaseManager.Instance.GetCurrentDisplayName(),
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await dbReference.Child("Users").Child(senderUID).Child("Notifications").Push()
                    .UpdateChildrenAsync(notificaiton);
                await targetInviteRef.RemoveValueAsync();

                string currentSceneName = SceneManager.GetActiveScene().name;
                if (currentSceneName != "LobbyScene")
                {
                    SceneManager.LoadScene("LobbyScene");
                    await Task.Delay(100); // 씬 로드 완료를 위한 짧은 대기
                }

                UIManager.Instance.CloseAllPanels();
                UIManager.Instance.OpenPanel(PanelType.Lobby);
                await ChannelManager.Instance.JoinChannel(targetServer);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 수락 실패: {ex.Message}\n{ex.StackTrace}");
                LogManager.Instance.ShowLog("채널 입장에 실패했습니다.");
            }
        }

        private async Task DeclineInvite(string senderUID)
        {
            await invitesRef.Child(senderUID).RemoveValueAsync();
            var notification = new Dictionary<string, object>
            {
                ["type"] = "invite_request_declined",
                ["decliner"] = FirebaseManager.Instance.CurrentUserUID,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            await dbReference.Child("Users").Child(senderUID).Child("Notifications").Push()
                .UpdateChildrenAsync(notification);
        }

        #endregion

        private void OnApplicationQuit()
        {
            StopListening();
        }
    }
}