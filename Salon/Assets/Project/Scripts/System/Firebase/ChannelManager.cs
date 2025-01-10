using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Salon.Firebase.Database;
using System.Threading.Tasks;

namespace Salon.Firebase
{
    public class ChannelManager : MonoBehaviour
    {
        private DatabaseReference dbReference;
        private string currentChannel;

        private async void OnEnable()
        {
            try
            {
                Debug.Log("ChannelManager OnEnable 시작");
                if (dbReference == null)
                {
                    dbReference = await GetDbReference();
                    Debug.Log("Firebase 데이터베이스 참조 설정 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Firebase 초기화 실패: {ex.Message}");
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

                Debug.Log($"Firebase 데이터베이스 참조 대기 중... (시도 {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("Firebase 데이터베이스 참조를 가져올 수 없습니다.");
        }

        public async Task ExistRooms()
        {
            try
            {
                var snapshot = await dbReference.Child("Channels").GetValueAsync();
                var existingRooms = new HashSet<string>();

                if (snapshot.Exists)
                {
                    foreach (var room in snapshot.Children)
                    {
                        existingRooms.Add(room.Key);
                    }
                }

                await CreateMissingRooms(existingRooms);
            }
            catch (Exception ex)
            {
                Debug.LogError($"채널 목록 확인 실패: {ex.Message}");
            }
        }

        private async Task CreateMissingRooms(HashSet<string> existingRooms)
        {
            for (int i = 1; i <= 10; i++)
            {
                string roomName = $"Channel{i}";
                if (!existingRooms.Contains(roomName))
                {
                    try
                    {
                        var roomData = new ChannelData();
                        string roomJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                        await dbReference.Child("Channels").Child(roomName).SetRawJsonValueAsync(roomJson);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"방 {roomName} 생성 중 오류: {ex.Message}");
                    }
                }
            }
        }

        public async Task AddPlayerToChannel(string channelName, string displayName)
        {
            try
            {
                Debug.Log("AddPlayerToRoom 돌입");
                var snapshot = await dbReference.Child("Channels").Child(channelName).GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogError($"채널 {channelName}이 존재하지 않습니다.");
                    throw new Exception($"채널 {channelName}이 존재하지 않습니다.");
                }

                int currentUserCount = snapshot.Child("CommonChannelData").Child("UserCount").Value != null ?
                    Convert.ToInt32(snapshot.Child("CommonChannelData").Child("UserCount").Value) : 0;

                if (currentUserCount >= 10)
                {
                    throw new Exception("채널이 가득 찼습니다.");
                }

                var playerData = new GamePlayerData(displayName);

                // Players에 플레이어 데이터 추가
                await dbReference.Child("Channels").Child(channelName).Child("Players")
                    .Child(displayName).SetRawJsonValueAsync(JsonConvert.SerializeObject(playerData));

                var ChannelUpdateData = new Dictionary<string, object>
                {
                    ["UserCount"] = currentUserCount + 1,

                    ["isFull"] = (currentUserCount + 1) >= 10
                };
                // CommonChannelData의 UserCount 업데이트
                await dbReference.Child("Channels").Child(channelName).Child("CommonChannelData").UpdateChildrenAsync(ChannelUpdateData);
                Debug.Log("플레이어 데이터 추가 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"플레이어 추가 중 오류: {ex.Message}");
                throw;
            }
        }

        public async Task RemovePlayerFromChannel(string channelName, string displayName)
        {
            try
            {
                var channelSnapshot = await dbReference.Child("Channels").Child(channelName).GetValueAsync();
                if (!channelSnapshot.Exists) return;

                await dbReference.Child("Channels").Child(channelName).Child("Players")
                    .Child(displayName).RemoveValueAsync();

                var userCountSnapshot = await dbReference.Child("Channels").Child(channelName)
                    .Child("CommonChannelData").Child("UserCount").GetValueAsync();

                int currentUserCount = userCountSnapshot.Value != null ?
                    Convert.ToInt32(userCountSnapshot.Value) : 0;

                if (currentUserCount > 0)
                {
                    var updates = new Dictionary<string, object>
                    {
                        ["UserCount"] = currentUserCount - 1,
                        ["isFull"] = (currentUserCount - 1) >= 10
                    };

                    await dbReference.Child("Channels").Child(channelName)
                        .Child("CommonChannelData").UpdateChildrenAsync(updates);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"플레이어 제거 중 오류: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, ChannelData>> WaitForChannelData()
        {
            try
            {
                var snapshot = await dbReference.Child("Channels").GetValueAsync();
                if (!snapshot.Exists) return null;

                var channelData = new Dictionary<string, ChannelData>();
                foreach (var channelSnapshot in snapshot.Children)
                {
                    channelData[channelSnapshot.Key] = JsonConvert.DeserializeObject<ChannelData>(channelSnapshot.GetRawJsonValue());
                }
                return channelData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"채널 데이터 로드 실패: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EnterChannel(string channelName)
        {
            try
            {
                print("EnterChannel 돌입");
                if (FirebaseManager.Instance.DbReference == null)
                {
                    print("DbReference 없음");
                    await Task.Delay(1000);
                    dbReference = await GetDbReference();
                }

                await AddPlayerToChannel(channelName, FirebaseManager.Instance.GetCurrentDisplayName());
                currentChannel = channelName;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"채널 입장 실패: {ex.Message}");
                return false;
            }
        }

        private async void OnApplicationQuit()
        {
            if (!string.IsNullOrEmpty(currentChannel))
            {
                try
                {
                    await RemovePlayerFromChannel(currentChannel, FirebaseManager.Instance.GetCurrentDisplayName());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"플레이어 제거 실패: {ex.Message}");
                }
            }
        }
    }
}