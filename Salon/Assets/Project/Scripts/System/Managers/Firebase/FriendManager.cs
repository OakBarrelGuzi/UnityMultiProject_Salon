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
using System.Linq;

namespace Salon.Firebase
{
    public class FriendManager : Singleton<FriendManager>
    {
        #region Fields
        private DatabaseReference dbReference;
        private DatabaseReference invitesRef;
        private Query invitesQuery;
        private DatabaseReference friendRequestsRef;

        public UnityEvent<string, string, string> OnInviteReceived = new UnityEvent<string, string, string>();
        public UnityEvent<string> OnFriendRequestReceived = new UnityEvent<string>();
        #endregion

        #region Initialization
        public bool isInitialized = false;

        public async Task Initialize()
        {
            if (isInitialized) return;

            try
            {
                Debug.Log("[FriendManager] 초기화 시작");
                dbReference = await GetDbReference();
                await SetupReferences();
                await CheckPendingRequests();
                isInitialized = true;
                Debug.Log("[FriendManager] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초기화 실패: {ex.Message}");
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

                Debug.Log($"[FriendManager] Firebase 데이터베이스 참조 대기 중... (시도 {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("[FriendManager] Firebase 데이터베이스 참조를 가져올 수 없습니다.");
        }

        private async Task SetupReferences()
        {
            try
            {
                string currentUser = FirebaseManager.Instance.CurrentUserName;
                if (string.IsNullOrEmpty(currentUser))
                {
                    Debug.LogWarning("[FriendManager] 현재 로그인된 사용자가 없어 초기화를 건너뜁니다.");
                    isInitialized = true;  // 로그인되지 않은 상태도 초기화된 상태로 간주
                    return;
                }

                // 기존 리스너 제거
                StopListening();

                var userRef = dbReference.Child("Users").Child(currentUser);

                // Invites 설정
                invitesRef = userRef.Child("Invites");
                invitesQuery = invitesRef.OrderByChild("timestamp");

                var invitesSnapshot = await invitesRef.GetValueAsync();
                if (!invitesSnapshot.Exists)
                {
                    Debug.Log("[FriendManager] Invites 노드가 없습니다. 새로 생성합니다.");
                    await invitesRef.SetValueAsync("");
                }

                // FriendRequests 설정
                friendRequestsRef = userRef.Child("FriendRequests");

                // FriendRequests 노드 존재 여부 확인 및 생성
                var friendRequestsSnapshot = await friendRequestsRef.GetValueAsync();
                if (!friendRequestsSnapshot.Exists)
                {
                    Debug.Log("[FriendManager] FriendRequests 노드가 없습니다. 새로 생성합니다.");
                    await friendRequestsRef.SetValueAsync("");
                }

                Debug.Log("[FriendManager] 데이터베이스 참조 설정 완료");
                Debug.Log($"[FriendManager] FriendRequests 경로: {friendRequestsRef.Reference.ToString()}");

                // 새로운 리스너 등록
                StartListening();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 데이터베이스 참조 설정 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        private void StartListening()
        {
            try
            {
                Debug.Log("[FriendManager] 이벤트 리스너 설정 시작");

                if (invitesQuery != null)
                {
                    invitesQuery.ChildAdded += OnInviteAdded;
                    Debug.Log("[FriendManager] 초대 메시지 리스닝 시작");
                }

                if (friendRequestsRef != null)  // Query 대신 Reference 사용
                {
                    friendRequestsRef.ChildAdded += OnFriendRequestAdded;
                    Debug.Log("[FriendManager] 친구 요청 리스닝 시작");
                }

                Debug.Log("[FriendManager] 이벤트 리스너 설정 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 이벤트 리스너 설정 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }

        private void StopListening()
        {
            try
            {
                Debug.Log("[FriendManager] 이벤트 리스너 제거 시작");

                if (invitesQuery != null)
                {
                    invitesQuery.ChildAdded -= OnInviteAdded;
                    Debug.Log("[FriendManager] 초대 메시지 리스닝 중지");
                }

                if (friendRequestsRef != null)  // Query 대신 Reference 사용
                {
                    friendRequestsRef.ChildAdded -= OnFriendRequestAdded;
                    Debug.Log("[FriendManager] 친구 요청 리스닝 중지");
                }

                Debug.Log("[FriendManager] 이벤트 리스너 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 이벤트 리스너 제거 중 오류 발생: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers
        private void OnInviteAdded(object sender, ChildChangedEventArgs args)
        {
            if (!args.Snapshot.Exists) return;

            try
            {
                var inviteData = JsonConvert.DeserializeObject<InviteData>(args.Snapshot.GetRawJsonValue());
                if (inviteData.Status == InviteStatus.Pending)
                {
                    OnInviteReceived.Invoke(inviteData.Inviter, inviteData.ChannelName, args.Snapshot.Key);
                    ShowInvitePopUp(inviteData, args.Snapshot.Key);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 처리 중 오류 발생: {ex.Message}");
            }
        }

        private void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
        {
            if (!args.Snapshot.Exists) return;

            try
            {
                Debug.Log($"[FriendManager] 새로운 친구 요청 감지: {args.Snapshot.Key}");
                Debug.Log($"[FriendManager] 친구 요청 데이터: {args.Snapshot.GetRawJsonValue()}");

                var requestData = JsonConvert.DeserializeObject<FriendRequestData>(args.Snapshot.GetRawJsonValue());
                if (requestData == null) return;

                // 내가 보낸 요청인지 확인
                if (requestData.sender == FirebaseManager.Instance.CurrentUserName)
                {
                    Debug.Log("[FriendManager] 자신이 보낸 친구 요청이므로 팝업을 표시하지 않습니다.");
                    return;
                }

                string senderServerName = args.Snapshot.Key;
                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);

                // pending 상태일 때만 팝업을 표시
                if (requestData.status == "pending")
                {
                    Debug.Log($"[FriendManager] 친구 요청 팝업 표시 - 발신자: {senderDisplayName} (서버명: {senderServerName})");
                    OnFriendRequestReceived.Invoke(senderDisplayName);

                    // 팝업 표시 전에 이전 팝업들을 모두 제거
                    PopUpManager.Instance.ClearQueue();
                    ShowFriendRequestPopUp(senderDisplayName, senderServerName);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 처리 중 오류 발생: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }
        #endregion

        #region PopUp Handlers
        private void ShowInvitePopUp(InviteData inviteData, string inviteKey)
        {
            PopUpManager.Instance.ShowPopUp(
                $"{inviteData.Inviter}님이 {inviteData.ChannelName} 채널로 초대했습니다.",
                async () => await AcceptInvite(inviteData.Inviter, inviteData.ChannelName, inviteKey),
                async () => await RemoveInvite(inviteKey)
            );
        }

        private void ShowFriendRequestPopUp(string senderDisplayName, string senderServerName)
        {
            Debug.Log($"[FriendManager] 친구 요청 팝업 생성 - 발신자: {senderDisplayName}, 서버명: {senderServerName}");
            PopUpManager.Instance.ShowPopUp(
                $"{senderDisplayName}님이 친구 요청을 보냈습니다.",
                async () => await AcceptFriendRequest(senderServerName),
                async () => await DeclineFriendRequest(senderServerName)
            );
        }
        #endregion

        #region Friend Request Methods
        private async Task<bool> SendFriendRequestToDatabase(string currentUserServerName, string serverFriendName, Dictionary<string, object> requestData)
        {
            try
            {
                var friendRequestsRef = dbReference.Child("Users").Child(serverFriendName).Child("FriendRequests");

                // FriendRequests 노드가 있는지 확인
                var snapshot = await friendRequestsRef.GetValueAsync();
                if (!snapshot.Exists)
                {
                    // FriendRequests 노드가 없는 경우, 빈 객체로 초기화
                    Debug.Log("[FriendManager] FriendRequests 노드 초기화");
                    await friendRequestsRef.SetValueAsync(new Dictionary<string, object>());
                }

                // 친구 요청 데이터 추가
                await friendRequestsRef.Child(currentUserServerName).UpdateChildrenAsync(requestData);
                Debug.Log("[FriendManager] 친구 요청 데이터 쓰기 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 데이터 쓰기 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SendFriendRequest(string friendDisplayName)
        {
            try
            {
                Debug.Log($"[FriendManager] 친구 요청 시작 - 대상: {friendDisplayName}");

                if (!isInitialized)
                {
                    Debug.Log("[FriendManager] 초기화되지 않음, 초기화 시작");
                    await Initialize();
                }

                Debug.Log("[FriendManager] 친구 요청 유효성 검사 시작");
                if (!IsValidFriendRequest(friendDisplayName))
                {
                    Debug.Log("[FriendManager] 친구 요청 유효성 검사 실패");
                    return false;
                }

                string serverFriendName = DisplayNameUtils.ToServerFormat(friendDisplayName);
                Debug.Log($"[FriendManager] 서버 형식 이름 변환: {friendDisplayName} -> {serverFriendName}");

                Debug.Log("[FriendManager] 사용자 존재 여부 확인 시작");
                if (!await ValidateUserExists(serverFriendName))
                {
                    Debug.Log("[FriendManager] 사용자 존재 여부 확인 실패");
                    return false;
                }

                string currentUserServerName = FirebaseManager.Instance.CurrentUserName;
                Debug.Log($"[FriendManager] 현재 사용자: {currentUserServerName}");

                if (friendDisplayName == currentUserServerName)
                {
                    LogManager.Instance.ShowLog("자신을 친구로 등록할 수 없습니다.");
                    return false;
                }

                try
                {
                    Debug.Log("[FriendManager] 기존 친구/요청 확인 시작");
                    if (await IsFriendOrRequestExists(currentUserServerName, serverFriendName))
                    {
                        Debug.Log("[FriendManager] 기존 친구/요청 확인 결과: 이미 존재함");
                        return false;
                    }

                    Debug.Log("[FriendManager] 친구 요청 데이터 준비 시작");
                    var requestData = new Dictionary<string, object>
                    {
                        ["sender"] = currentUserServerName,
                        ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ["status"] = "pending"
                    };

                    Debug.Log($"[FriendManager] 요청 데이터 준비 완료: {JsonConvert.SerializeObject(requestData)}");

                    bool success = await SendFriendRequestToDatabase(currentUserServerName, serverFriendName, requestData);
                    if (success)
                    {
                        LogManager.Instance.ShowLog($"{friendDisplayName}님에게 친구 요청을 보냈습니다.");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[FriendManager] 데이터베이스 작업 중 오류: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                return false;
            }
        }

        private async Task<bool> ValidateUserExists(string serverFriendName)
        {
            try
            {
                Debug.Log($"[FriendManager] 사용자 존재 여부 확인: {serverFriendName}");
                var snapshot = await dbReference.Child("Users").Child(serverFriendName).GetValueAsync();
                Debug.Log($"[FriendManager] 사용자 데이터 스냅샷 존재 여부: {snapshot.Exists}");
                if (!snapshot.Exists)
                {
                    LogManager.Instance.ShowLog("존재하지 않는 사용자입니다.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 사용자 확인 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                return false;
            }
        }

        private bool IsValidFriendRequest(string friendDisplayName)
        {
            if (!DisplayNameUtils.IsValidDisplayName(friendDisplayName))
            {
                LogManager.Instance.ShowLog("올바르지 않은 사용자 ID 형식입니다.");
                return false;
            }

            if (friendDisplayName == FirebaseManager.Instance.GetCurrentDisplayName())
            {
                LogManager.Instance.ShowLog("자신을 친구로 등록할 수 없습니다.");
                return false;
            }

            return true;
        }

        private async Task<bool> IsFriendOrRequestExists(string currentUserServerName, string serverFriendName)
        {
            try
            {
                Debug.Log($"[FriendManager] 친구 관계 확인 시작 - 현재 사용자: {currentUserServerName}, 대상: {serverFriendName}");

                // 1. 친구 관계 확인
                var friendSnapshot = await dbReference.Child("Users").Child(currentUserServerName).Child("Friends").Child(serverFriendName).GetValueAsync();
                Debug.Log($"[FriendManager] 친구 관계 확인 결과: {friendSnapshot.Exists}");
                if (friendSnapshot.Exists)
                {
                    LogManager.Instance.ShowLog("이미 친구로 등록된 사용자입니다.");
                    return true;
                }

                // 2. 보낸 요청 확인
                Debug.Log("[FriendManager] 보낸 요청 확인 시작");
                try
                {
                    var friendRequestsSnapshot = await dbReference.Child("Users").Child(serverFriendName).Child("FriendRequests").GetValueAsync();
                    Debug.Log($"[FriendManager] FriendRequests 노드 존재 여부: {friendRequestsSnapshot.Exists}");

                    if (friendRequestsSnapshot.Exists)
                    {
                        var sentRequest = friendRequestsSnapshot.Child(currentUserServerName);
                        Debug.Log($"[FriendManager] 보낸 요청 확인 결과: {sentRequest.Exists}");

                        if (sentRequest.Exists)
                        {
                            var requestData = JsonConvert.DeserializeObject<FriendRequestData>(sentRequest.GetRawJsonValue());
                            Debug.Log($"[FriendManager] 보낸 요청 상태: {requestData?.status ?? "null"}");
                            if (requestData?.status == "pending")
                            {
                                LogManager.Instance.ShowLog("이미 친구 요청을 보낸 사용자입니다.");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FriendManager] 보낸 요청 확인 중 오류 (무시됨): {ex.Message}");
                }

                // 3. 받은 요청 확인
                Debug.Log("[FriendManager] 받은 요청 확인 시작");
                try
                {
                    var myFriendRequestsSnapshot = await dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").GetValueAsync();
                    Debug.Log($"[FriendManager] 내 FriendRequests 노드 존재 여부: {myFriendRequestsSnapshot.Exists}");

                    if (myFriendRequestsSnapshot.Exists)
                    {
                        var receivedRequest = myFriendRequestsSnapshot.Child(serverFriendName);
                        Debug.Log($"[FriendManager] 받은 요청 확인 결과: {receivedRequest.Exists}");

                        if (receivedRequest.Exists)
                        {
                            var requestData = JsonConvert.DeserializeObject<FriendRequestData>(receivedRequest.GetRawJsonValue());
                            Debug.Log($"[FriendManager] 받은 요청 상태: {requestData?.status ?? "null"}");
                            if (requestData?.status == "pending")
                            {
                                LogManager.Instance.ShowLog("상대방이 이미 친구 요청을 보냈습니다.");
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FriendManager] 받은 요청 확인 중 오류 (무시됨): {ex.Message}");
                }

                Debug.Log("[FriendManager] 기존 친구/요청 없음, 친구 요청 진행 가능");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구/요청 확인 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }
        #endregion

        #region Friend Request Response Methods
        private async Task RemoveFriendRequest(string senderServerName)
        {
            try
            {
                Debug.Log($"[FriendManager] 친구 요청 제거 시작 - 발신자: {senderServerName}");
                string currentUserServerName = FirebaseManager.Instance.CurrentUserName;
                var requestRef = dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").Child(senderServerName);
                await requestRef.RemoveValueAsync();
                Debug.Log($"[FriendManager] 친구 요청 제거 완료 - 발신자: {senderServerName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 제거 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> AcceptFriendRequest(string senderServerName)
        {
            try
            {
                Debug.Log($"[FriendManager] 친구 요청 수락 시작 - 발신자: {senderServerName}");
                string currentUserServerName = FirebaseManager.Instance.CurrentUserName;

                if (!await ValidateFriendRequest(currentUserServerName, senderServerName))
                {
                    Debug.Log("[FriendManager] 친구 요청 유효성 검사 실패");
                    return false;
                }

                // 먼저 상태를 accepted로 변경
                await UpdateRequestStatus(senderServerName, "accepted");

                Debug.Log("[FriendManager] 친구 관계 업데이트 시작");
                await UpdateFriendshipStatus(currentUserServerName, senderServerName);

                Debug.Log("[FriendManager] 수락 알림 전송 시작");
                await SendAcceptanceNotification(currentUserServerName, senderServerName);

                Debug.Log("[FriendManager] 친구 요청 제거 시작");
                await RemoveFriendRequest(senderServerName);

                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);
                LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 수락했습니다.");
                Debug.Log($"[FriendManager] 친구 요청 수락 완료 - 발신자: {senderServerName}");

                // 팝업 큐 초기화
                PopUpManager.Instance.ClearQueue();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 수락 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                LogManager.Instance.ShowLog("친구 요청 수락에 실패했습니다.");
                return false;
            }
        }

        public async Task<bool> DeclineFriendRequest(string senderServerName)
        {
            try
            {
                Debug.Log($"[FriendManager] 친구 요청 거절 시작 - 발신자: {senderServerName}");
                string currentUserServerName = FirebaseManager.Instance.CurrentUserName;

                if (!await ValidateFriendRequest(currentUserServerName, senderServerName))
                {
                    Debug.Log("[FriendManager] 친구 요청 유효성 검사 실패");
                    return false;
                }

                // 먼저 상태를 declined로 변경
                await UpdateRequestStatus(senderServerName, "declined");

                Debug.Log("[FriendManager] 친구 요청 제거 시작");
                await RemoveFriendRequest(senderServerName);

                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);
                LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 거절했습니다.");
                Debug.Log($"[FriendManager] 친구 요청 거절 완료 - 발신자: {senderServerName}");

                // 팝업 큐 초기화
                PopUpManager.Instance.ClearQueue();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 거절 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                LogManager.Instance.ShowLog("친구 요청 거절에 실패했습니다.");
                return false;
            }
        }

        private async Task<bool> ValidateFriendRequest(string currentUserServerName, string senderServerName)
        {
            var requestSnapshot = await dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").Child(senderServerName).GetValueAsync();
            if (!requestSnapshot.Exists)
            {
                LogManager.Instance.ShowLog("존재하지 않는 친구 요청입니다.");
                return false;
            }

            var requestData = JsonConvert.DeserializeObject<FriendRequestData>(requestSnapshot.GetRawJsonValue());
            if (requestData.status != "pending")
            {
                LogManager.Instance.ShowLog("이미 처리된 친구 요청입니다.");
                return false;
            }

            return true;
        }
        #endregion

        #region Database Operations
        private async Task SendFriendRequest(string currentUserServerName, string serverFriendName)
        {
            var requestData = new Dictionary<string, object>
            {
                ["sender"] = currentUserServerName,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["status"] = "pending"
            };

            await dbReference.Child("Users").Child(serverFriendName).Child("FriendRequests").Child(currentUserServerName).UpdateChildrenAsync(requestData);
        }

        private async Task UpdateFriendshipStatus(string currentUserServerName, string senderServerName)
        {
            var updates = new Dictionary<string, object>
            {
                [$"Users/{currentUserServerName}/Friends/{senderServerName}"] = true,
                [$"Users/{senderServerName}/Friends/{currentUserServerName}"] = true,
                [$"Users/{currentUserServerName}/FriendRequests/{senderServerName}/status"] = "accepted",
                [$"Users/{currentUserServerName}/FriendRequests/{senderServerName}/acceptedTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await dbReference.UpdateChildrenAsync(updates);
        }

        private async Task SendAcceptanceNotification(string currentUserServerName, string senderServerName)
        {
            var notificationData = new Dictionary<string, object>
            {
                ["type"] = "friend_request_accepted",
                ["accepter"] = currentUserServerName,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await dbReference.Child("Users").Child(senderServerName).Child("Notifications").Push().UpdateChildrenAsync(notificationData);
        }

        private async Task UpdateRequestStatus(string senderServerName, string status)
        {
            var updates = new Dictionary<string, object>
            {
                ["status"] = status,
                [$"{status}Time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            string currentUserServerName = FirebaseManager.Instance.CurrentUserName;
            await dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").Child(senderServerName).UpdateChildrenAsync(updates);
        }

        #endregion

        #region Invite Methods
        public async Task SendInvite(string friendId, string channelName)
        {
            try
            {
                var inviteData = new InviteData(FirebaseManager.Instance.CurrentUserName, channelName);
                string inviteJson = JsonConvert.SerializeObject(inviteData);

                // 이미 서버 형식으로 전달받은 friendId를 사용
                await dbReference.Child("Users").Child(friendId).Child("Invites").Push().SetRawJsonValueAsync(inviteJson);

                string friendDisplayName = DisplayNameUtils.ToDisplayFormat(friendId);
                LogManager.Instance.ShowLog($"{friendDisplayName}님을 채널로 초대했습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 전송 실패: {ex.Message}");
                LogManager.Instance.ShowLog("초대 전송에 실패했습니다.");
            }
        }

        public async Task AcceptInvite(string inviter, string channelName, string inviteKey)
        {
            try
            {
                var updates = new Dictionary<string, object>
                {
                    ["Status"] = InviteStatus.Accepted,
                    ["AcceptedTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await invitesRef.Child(inviteKey).UpdateChildrenAsync(updates);

                var notificationData = new Dictionary<string, object>
                {
                    ["type"] = "invite_accepted",
                    ["accepter"] = FirebaseManager.Instance.CurrentUserName,
                    ["channelName"] = channelName,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                await dbReference.Child("Users").Child(inviter).Child("Notifications").Push().UpdateChildrenAsync(notificationData);
                LogManager.Instance.ShowLog($"{inviter}님의 초대를 수락했습니다. 채널 목록에서 {channelName}에 입장할 수 있습니다.");

                await Task.Delay(3000);
                await RemoveInvite(inviteKey);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 수락 처리 중 오류 발생: {ex.Message}");
                LogManager.Instance.ShowLog("초대 수락에 실패했습니다.");
            }
        }

        public async Task RemoveInvite(string inviteKey)
        {
            try
            {
                if (string.IsNullOrEmpty(inviteKey) || invitesRef == null) return;
                await invitesRef.Child(inviteKey).RemoveValueAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 제거 중 오류 발생: {ex.Message}");
            }
        }
        #endregion

        #region Friend List Methods
        public async Task<Dictionary<string, UserData>> GetFriendsList()
        {
            try
            {
                string currentUser = FirebaseManager.Instance.CurrentUserName;
                var snapshot = await dbReference.Child("Users").Child(currentUser).Child("Friends").GetValueAsync();

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

        private async Task CheckPendingRequests()
        {
            try
            {
                Debug.Log("[FriendManager] 대기 중인 요청 확인 시작");

                // 친구 요청 확인
                var friendRequests = await GetFriendRequests();
                if (friendRequests != null)
                {
                    foreach (var request in friendRequests)
                    {
                        Debug.Log($"[FriendManager] 대기 중인 친구 요청 발견: {request.Key}");
                        OnFriendRequestReceived.Invoke(request.Key);
                        ShowFriendRequestPopUp(request.Key, DisplayNameUtils.ToServerFormat(request.Key));
                    }
                }

                // 초대 확인
                if (invitesRef != null)  // invitesRef가 초기화되었는지 확인
                {
                    var invites = await GetInvites();
                    if (invites != null)
                    {
                        foreach (var invite in invites)
                        {
                            Debug.Log($"[FriendManager] 대기 중인 초대 발견: {invite.Key}");
                            OnInviteReceived.Invoke(invite.Value.Inviter, invite.Value.ChannelName, invite.Key);
                            ShowInvitePopUp(invite.Value, invite.Key);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[FriendManager] 초대 참조가 초기화되지 않아 초대 확인을 건너뜁니다.");
                }

                Debug.Log("[FriendManager] 대기 중인 요청 확인 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 대기 중인 요청 확인 중 오류 발생: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }

        public async Task<Dictionary<string, InviteData>> GetInvites()
        {
            try
            {
                if (invitesRef == null)
                {
                    Debug.LogWarning("[FriendManager] 초대 참조가 초기화되지 않았습니다.");
                    return new Dictionary<string, InviteData>();
                }

                var snapshot = await invitesRef.GetValueAsync();
                var invites = new Dictionary<string, InviteData>();

                if (snapshot != null && snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        try
                        {
                            var inviteData = JsonConvert.DeserializeObject<InviteData>(child.GetRawJsonValue());
                            if (inviteData != null)
                            {
                                invites[child.Key] = inviteData;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[FriendManager] 초대 데이터 파싱 실패: {ex.Message}");
                            continue;
                        }
                    }
                }
                return invites;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 초대 목록 조회 실패: {ex.Message}");
                return new Dictionary<string, InviteData>();
            }
        }

        private async Task<Dictionary<string, FriendRequestData>> GetFriendRequests()
        {
            try
            {
                string currentUserServerName = FirebaseManager.Instance.CurrentUserName;
                if (string.IsNullOrEmpty(currentUserServerName))
                {
                    Debug.LogWarning("[FriendManager] 현재 사용자 이름이 없어 친구 요청을 조회할 수 없습니다.");
                    return new Dictionary<string, FriendRequestData>();
                }

                if (dbReference == null)
                {
                    Debug.LogWarning("[FriendManager] 데이터베이스 참조가 없습니다.");
                    return new Dictionary<string, FriendRequestData>();
                }

                var friendRequestsRef = dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests");
                var snapshot = await friendRequestsRef.GetValueAsync();

                var requests = new Dictionary<string, FriendRequestData>();
                if (snapshot != null && snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        try
                        {
                            var requestData = JsonConvert.DeserializeObject<FriendRequestData>(child.GetRawJsonValue());
                            if (requestData != null)
                            {
                                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(child.Key);
                                requests[senderDisplayName] = requestData;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[FriendManager] 친구 요청 데이터 파싱 실패: {ex.Message}");
                            continue;
                        }
                    }
                }
                return requests;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FriendManager] 친구 요청 목록 조회 실패: {ex.Message}");
                return new Dictionary<string, FriendRequestData>();
            }
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            StopListening();
        }
        #endregion
    }
}