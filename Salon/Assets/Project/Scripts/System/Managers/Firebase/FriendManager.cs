using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Salon.Firebase;
using Salon.Firebase.Database;
using Salon.System;
using UnityEngine.Events;

public class FriendManager : Singleton<FriendManager>
{
    private DatabaseReference dbReference;
    private DatabaseReference invitesRef;
    private Query invitesQuery;
    private DatabaseReference friendRequestsRef;
    private Query friendRequestsQuery;

    public UnityEvent<string, string, string> OnInviteReceived = new UnityEvent<string, string, string>();
    public UnityEvent<string> OnFriendRequestReceived = new UnityEvent<string>();

    public async void Initialize()
    {
        await WaitForFirebaseInitialization();
        SetupReferences();
        StartListeningToInvites();
        StartListeningToFriendRequests();
        await CheckPendingRequests();
    }

    private async Task WaitForFirebaseInitialization()
    {
        while (!FirebaseManager.Instance.IsInitialized)
        {
            await Task.Delay(100);
        }
        dbReference = FirebaseManager.Instance.DbReference;
    }

    private void SetupReferences()
    {
        string currentUser = FirebaseManager.Instance.CurrentUserName;
        if (string.IsNullOrEmpty(currentUser)) return;

        invitesRef = dbReference.Child("Users").Child(currentUser).Child("Invites");
        invitesQuery = invitesRef.OrderByChild("timestamp");

        friendRequestsRef = dbReference.Child("Users").Child(currentUser).Child("FriendRequests");
        friendRequestsQuery = friendRequestsRef.OrderByChild("timestamp");
    }

    private void StartListeningToInvites()
    {
        if (invitesQuery == null) return;

        invitesQuery.ChildAdded += OnInviteAdded;
        Debug.Log("[FriendManager] 초대 메시지 리스닝 시작");
    }

    private void StopListeningToInvites()
    {
        if (invitesQuery != null)
        {
            invitesQuery.ChildAdded -= OnInviteAdded;
            Debug.Log("[FriendManager] 초대 메시지 리스닝 중지");
        }
    }

    private void OnInviteAdded(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        try
        {
            var inviteData = JsonConvert.DeserializeObject<InviteData>(args.Snapshot.GetRawJsonValue());
            if (inviteData.Status == InviteStatus.Pending)
            {
                ShowInvitePopup(inviteData.Inviter, inviteData.ChannelName, args.Snapshot.Key);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 초대 처리 중 오류 발생: {ex.Message}");
        }
    }

    private void ShowInvitePopup(string inviter, string channelName, string inviteKey)
    {
        OnInviteReceived.Invoke(inviter, channelName, inviteKey);
        UIManager.Instance.OpenPanel(PanelType.PopUp);
        var popupPanel = UIManager.Instance.GetUI<PopUpPanel>();
        if (popupPanel != null)
        {
            string message = $"{inviter}님이 {channelName} 채널로 초대했습니다.\n초대를 수락하시겠습니까?";
            popupPanel.Initialize(async () =>
            {
                await AcceptInvite(inviter, channelName, inviteKey);
            }, message);
        }
    }

    private async Task AcceptInvite(string inviter, string channelName, string inviteKey)
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

    public async Task DeclineInvite(string inviteKey)
    {
        try
        {
            var updates = new Dictionary<string, object>
            {
                ["Status"] = InviteStatus.Declined,
                ["DeclinedTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            await invitesRef.Child(inviteKey).UpdateChildrenAsync(updates);

            await Task.Delay(1000);
            await RemoveInvite(inviteKey);
            LogManager.Instance.ShowLog("초대를 거절했습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 초대 거절 처리 중 오류 발생: {ex.Message}");
        }
    }
    public async Task<Dictionary<string, UserData>> GetFriendsList()
    {
        try
        {
            var currentUser = FirebaseManager.Instance.CurrentUserName;

            var userSnapshot = await dbReference.Child("Users").Child(currentUser).GetValueAsync();
            if (!userSnapshot.Exists)
            {
                Debug.Log("[FriendManager] 사용자 데이터가 없습니다.");
                return new Dictionary<string, UserData>();
            }

            if (!userSnapshot.HasChild("Friends"))
            {
                Debug.Log("[FriendManager] Friends 노드가 없습니다.");
                return new Dictionary<string, UserData>();
            }

            var friendsList = new Dictionary<string, UserData>();
            var friendsSnapshot = userSnapshot.Child("Friends");

            foreach (var child in friendsSnapshot.Children)
            {
                var friendData = await GetFriendData(child.Key);
                if (friendData != null)
                {
                    string displayName = DisplayNameUtils.ToDisplayFormat(child.Key);
                    friendsList[displayName] = friendData;
                }
            }
            return friendsList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 목록 조회 실패: {ex.Message}");
            return null;
        }
    }

    public async Task SendInvite(string friendId, string channelName)
    {
        try
        {
            var inviteData = new InviteData(FirebaseManager.Instance.CurrentUserName, channelName);
            string inviteJson = JsonConvert.SerializeObject(inviteData);

            await dbReference.Child("Users").Child(friendId).Child("Invites").Push().SetRawJsonValueAsync(inviteJson);
            LogManager.Instance.ShowLog($"{friendId}님을 채널로 초대했습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 초대 전송 실패: {ex.Message}");
            LogManager.Instance.ShowLog("초대 전송에 실패했습니다.");
        }
    }

    public async Task RemoveInvite(string inviteKey)
    {
        try
        {
            if (string.IsNullOrEmpty(inviteKey))
            {
                Debug.LogWarning("[FriendManager] 삭제할 초대 키가 null입니다.");
                return;
            }

            var inviteSnapshot = await invitesRef.Child(inviteKey).GetValueAsync();
            if (!inviteSnapshot.Exists)
            {
                Debug.LogWarning("[FriendManager] 이미 삭제된 초대입니다.");
                return;
            }

            await invitesRef.Child(inviteKey).RemoveValueAsync();
            Debug.Log($"[FriendManager] 초대 메시지 삭제 완료: {inviteKey}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 초대 제거 중 오류 발생: {ex.Message}");
        }
    }

    public async Task<bool> AddFriend(string friendDisplayName)
    {
        try
        {
            if (!DisplayNameUtils.IsValidDisplayName(friendDisplayName))
            {
                LogManager.Instance.ShowLog("올바르지 않은 사용자 ID 형식입니다.");
                return false;
            }

            string serverFriendName = DisplayNameUtils.ToServerFormat(friendDisplayName);

            var userData = await FirebaseManager.Instance.GetUserDataAsync(serverFriendName);
            if (userData == null)
            {
                LogManager.Instance.ShowLog("존재하지 않는 사용자입니다.");
                return false;
            }

            string currentUserServerName = FirebaseManager.Instance.CurrentUserName;

            // 이미 친구인지 확인
            var friendSnapshot = await dbReference.Child("Users").Child(currentUserServerName).Child("Friends").Child(serverFriendName).GetValueAsync();
            if (friendSnapshot.Exists)
            {
                LogManager.Instance.ShowLog("이미 친구로 등록된 사용자입니다.");
                return false;
            }

            // 이미 보낸 요청이 있는지 확인
            var sentRequestSnapshot = await dbReference.Child("Users").Child(serverFriendName).Child("FriendRequests").Child(currentUserServerName).GetValueAsync();
            if (sentRequestSnapshot.Exists)
            {
                LogManager.Instance.ShowLog("이미 친구 요청을 보낸 사용자입니다.");
                return false;
            }

            // 친구 요청 데이터 생성
            var requestData = new Dictionary<string, object>
            {
                ["sender"] = currentUserServerName,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["status"] = "pending"
            };

            // 친구 요청 전송
            await dbReference.Child("Users").Child(serverFriendName).Child("FriendRequests").Child(currentUserServerName).UpdateChildrenAsync(requestData);
            LogManager.Instance.ShowLog($"{friendDisplayName}님에게 친구 요청을 보냈습니다.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 요청 실패: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AcceptFriendRequest(string senderServerName)
    {
        try
        {
            string currentUserServerName = FirebaseManager.Instance.CurrentUserName;

            // 친구 요청 수락 및 양쪽 친구 목록에 추가
            var updates = new Dictionary<string, object>
            {
                [$"Users/{currentUserServerName}/Friends/{senderServerName}"] = true,
                [$"Users/{senderServerName}/Friends/{currentUserServerName}"] = true,
                [$"Users/{currentUserServerName}/FriendRequests/{senderServerName}/status"] = "accepted",
                [$"Users/{currentUserServerName}/FriendRequests/{senderServerName}/acceptedTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await dbReference.UpdateChildrenAsync(updates);

            // 수락 알림 전송
            var notificationData = new Dictionary<string, object>
            {
                ["type"] = "friend_request_accepted",
                ["accepter"] = currentUserServerName,
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await dbReference.Child("Users").Child(senderServerName).Child("Notifications").Push().UpdateChildrenAsync(notificationData);

            string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);
            LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 수락했습니다.");

            await RemoveFriendRequest(senderServerName);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 요청 수락 실패: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeclineFriendRequest(string senderServerName)
    {
        try
        {
            string currentUserServerName = FirebaseManager.Instance.CurrentUserName;

            // 거절 상태 업데이트
            var updates = new Dictionary<string, object>
            {
                ["status"] = "declined",
                ["declinedTime"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").Child(senderServerName).UpdateChildrenAsync(updates);

            string senderDisplayName = DisplayNameUtils.ToDisplayFormat(senderServerName);
            LogManager.Instance.ShowLog($"{senderDisplayName}님의 친구 요청을 거절했습니다.");

            await RemoveFriendRequest(senderServerName);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 요청 거절 실패: {ex.Message}");
            return false;
        }
    }

    private async Task RemoveFriendRequest(string senderServerName)
    {
        try
        {
            if (string.IsNullOrEmpty(senderServerName))
            {
                Debug.LogWarning("[FriendManager] 삭제할 친구 요청의 발신자가 null입니다.");
                return;
            }

            string currentUserServerName = FirebaseManager.Instance.CurrentUserName;
            var requestRef = dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").Child(senderServerName);

            var requestSnapshot = await requestRef.GetValueAsync();
            if (!requestSnapshot.Exists)
            {
                Debug.LogWarning("[FriendManager] 이미 삭제된 친구 요청입니다.");
                return;
            }

            await requestRef.RemoveValueAsync();
            Debug.Log($"[FriendManager] 친구 요청 메시지 삭제 완료: {senderServerName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 요청 제거 중 오류 발생: {ex.Message}");
        }
    }

    public async Task<Dictionary<string, FriendRequestData>> GetFriendRequests()
    {
        try
        {
            string currentUserServerName = FirebaseManager.Instance.CurrentUserName;
            var snapshot = await dbReference.Child("Users").Child(currentUserServerName).Child("FriendRequests").GetValueAsync();

            var requests = new Dictionary<string, FriendRequestData>();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    var requestData = JsonConvert.DeserializeObject<FriendRequestData>(child.GetRawJsonValue());
                    string senderDisplayName = DisplayNameUtils.ToDisplayFormat(child.Key);
                    requests[senderDisplayName] = requestData;
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

    private async Task<UserData> GetFriendData(string friendId)
    {
        try
        {
            var snapshot = await dbReference.Child("Users").Child(friendId).GetValueAsync();
            if (snapshot.Exists)
            {
                return JsonConvert.DeserializeObject<UserData>(snapshot.GetRawJsonValue());
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 데이터 조회 실패: {ex.Message}");
            return null;
        }
    }

    private void StartListeningToFriendRequests()
    {
        if (friendRequestsQuery == null) return;

        friendRequestsQuery.ChildAdded += OnFriendRequestAdded;
        Debug.Log("[FriendManager] 친구 요청 리스닝 시작");
    }

    private void StopListeningToFriendRequests()
    {
        if (friendRequestsQuery != null)
        {
            friendRequestsQuery.ChildAdded -= OnFriendRequestAdded;
            Debug.Log("[FriendManager] 친구 요청 리스닝 중지");
        }
    }

    private void OnFriendRequestAdded(object sender, ChildChangedEventArgs args)
    {
        if (!args.Snapshot.Exists) return;

        try
        {
            var requestData = JsonConvert.DeserializeObject<FriendRequestData>(args.Snapshot.GetRawJsonValue());
            if (requestData.status == "pending")
            {
                string senderDisplayName = DisplayNameUtils.ToDisplayFormat(requestData.sender);
                OnFriendRequestReceived.Invoke(senderDisplayName);
                ShowFriendRequestPopup(senderDisplayName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 친구 요청 처리 중 오류 발생: {ex.Message}");
        }
    }

    private void ShowFriendRequestPopup(string senderDisplayName)
    {
        UIManager.Instance.OpenPanel(PanelType.PopUp);
        var popupPanel = UIManager.Instance.GetUI<PopUpPanel>();
        if (popupPanel != null)
        {
            string message = $"{senderDisplayName}님이 친구 요청을 보냈습니다.\n수락하시겠습니까?";
            string serverName = DisplayNameUtils.ToServerFormat(senderDisplayName);
            popupPanel.Initialize(async () =>
            {
                await AcceptFriendRequest(serverName);
            }, message);
        }
    }

    private async Task CheckPendingRequests()
    {
        try
        {
            var requests = await GetFriendRequests();
            foreach (var request in requests)
            {
                ShowFriendRequestPopup(request.Key);
                await Task.Delay(100); // 팝업이 겹치지 않도록 약간의 딜레이
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FriendManager] 대기 중인 친구 요청 확인 중 오류 발생: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        StopListeningToInvites();
        StopListeningToFriendRequests();
    }
}

public class FriendRequestData
{
    public string sender { get; set; }
    public long timestamp { get; set; }
    public string status { get; set; }
}
