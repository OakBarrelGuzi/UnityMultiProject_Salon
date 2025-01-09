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
                if (dbReference == null)
                {
                    dbReference = await GetDbReference();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Firebase 초기화 실패: {ex.Message}");
            }
        }

        private async Task<DatabaseReference> GetDbReference()
        {
            while (FirebaseManager.Instance.DbReference == null)
                await Task.Delay(100);
            return FirebaseManager.Instance.DbReference;
        }

        public async Task ExistRooms()
        {
            try
            {
                var snapshot = await dbReference.Child("Rooms").GetValueAsync();
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
                Debug.LogError($"방 목록 확인 실패: {ex.Message}");
            }
        }

        private async Task CreateMissingRooms(HashSet<string> existingRooms)
        {
            for (int i = 1; i <= 10; i++)
            {
                string roomName = $"Room{i}";
                if (!existingRooms.Contains(roomName))
                {
                    try
                    {
                        var roomData = new ChannelData();
                        string roomJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                        await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(roomJson);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"방 {roomName} 생성 중 오류: {ex.Message}");
                    }
                }
            }
        }

        public async Task AddPlayerToRoom(string roomName, string displayName)
        {
            try
            {
                var snapshot = await dbReference.Child("Rooms").Child(roomName).GetValueAsync();
                if (!snapshot.Exists) throw new Exception($"방 {roomName}이 존재하지 않습니다.");

                var roomData = JsonConvert.DeserializeObject<ChannelData>(snapshot.GetRawJsonValue());
                roomData.Players ??= new Dictionary<string, GamePlayerData>();

                if (roomData.Players.ContainsKey(displayName))
                {
                    Debug.Log($"플레이어 {displayName}는 이미 방 {roomName}에 존재합니다.");
                    return;
                }

                if (roomData.isFull)
                    throw new Exception("방이 가득 찼습니다.");

                roomData.Players[displayName] = new GamePlayerData(displayName);
                roomData.UserCount++;
                roomData.isFull = roomData.UserCount >= 10;

                string updatedJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"플레이어 추가 중 오류: {ex.Message}");
                throw;
            }
        }

        public async Task RemovePlayerFromRoom(string roomName, string displayName)
        {
            try
            {
                var snapshot = await dbReference.Child("Rooms").Child(roomName).GetValueAsync();
                if (!snapshot.Exists) return;

                var roomData = JsonConvert.DeserializeObject<ChannelData>(snapshot.GetRawJsonValue());
                if (roomData.Players?.Remove(displayName) ?? false)
                {
                    roomData.UserCount--;
                    roomData.isFull = roomData.UserCount >= 10;

                    string updatedJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                    await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson);
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
                var snapshot = await dbReference.Child("Rooms").GetValueAsync();
                if (!snapshot.Exists) return null;

                var loadRoomData = new Dictionary<string, ChannelData>();
                foreach (var roomSnapshot in snapshot.Children)
                {
                    loadRoomData[roomSnapshot.Key] = JsonConvert.DeserializeObject<ChannelData>(roomSnapshot.GetRawJsonValue());
                }
                return loadRoomData;
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
                // 1. 데이터베이스 참조 확인 및 대기
                if (FirebaseManager.Instance.DbReference == null)
                {
                    await Task.Delay(1000);
                    dbReference = await GetDbReference();
                }

                // 2. 플레이어 추가 완료까지 대기
                await AddPlayerToRoom(channelName, FirebaseManager.Instance.GetCurrentDisplayName());
                currentChannel = channelName;

                // 3. 성공 여부 반환
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
                    await RemovePlayerFromRoom(currentChannel, FirebaseManager.Instance.GetCurrentDisplayName());
                }
                catch (Exception ex)
                {
                    Debug.LogError($"플레이어 제거 실패: {ex.Message}");
                }
            }
        }
    }
}