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

        private async void OnEnable()
        {
            dbReference = await GetDbReference();
        }

        private async Task<DatabaseReference> GetDbReference()
        {
            while (FirebaseManager.Instance.DbReference == null)
                await Task.Delay(100);

            return FirebaseManager.Instance.DbReference;
        }

        private async Task ExistRooms()
        {
            Debug.Log("Firebase 방 생성 시작");

            try
            {
                DataSnapshot snapshot = await dbReference.Child("Rooms").GetValueAsync();

                // 이미 존재하는 방 이름 저장
                HashSet<string> existingRooms = new HashSet<string>();
                if (snapshot.Exists)
                {
                    foreach (DataSnapshot room in snapshot.Children)
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
            Debug.Log("Firebase 누락된 방 생성 시작");

            for (int i = 1; i <= 10; i++)
            {
                string roomName = $"Room{i}";

                if (false == existingRooms.Contains(roomName)) // 기존에 없는 방만 추가
                {
                    RoomData roomData = new RoomData();
                    string roomJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);

                    try
                    {
                        await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(roomJson);
                        Debug.Log($"방 {roomName} 생성 완료!");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"방 {roomName} 생성 중 오류 발생: {ex.Message}");
                    }
                }
                else
                {
                    Debug.Log($"방 {roomName}은 이미 존재합니다. 생성하지 않습니다.");
                }
            }
            Debug.Log("Firebase 방 생성 완료");
        }
        public async Task AddPlayerToRoom(string roomName, string displayName)
        {
            try
            {
                // Firebase에서 현재 방 데이터 가져오기
                DataSnapshot snapshot = await dbReference.Child("Rooms").Child(roomName).GetValueAsync();

                RoomData roomData;

                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    roomData = JsonConvert.DeserializeObject<RoomData>(json);
                }
                else
                {
                    Debug.LogError($"방 {roomName}이 존재하지 않습니다. 새로 생성해야 합니다.");
                    return;
                }

                // Players가 null인 경우 초기화
                if (roomData.Players == null)
                    roomData.Players = new Dictionary<string, GamePlayerData>();

                // 방이 가득 찼는지 확인
                if (roomData.isFull)
                {
                    Debug.Log($"방 {roomName}은 이미 가득 찼습니다. 플레이어 추가 불가.");
                    return;
                }

                // 플레이어 추가
                if (!roomData.Players.ContainsKey(displayName))
                {
                    GamePlayerData newPlayer = new GamePlayerData(displayName);
                    roomData.Players[displayName] = newPlayer;
                    roomData.UserCount++;

                    // 방이 가득 찼는지 확인
                    if (roomData.UserCount >= 10)
                        roomData.isFull = true;

                    Debug.Log($"플레이어 {displayName}가 방 {roomName}에 추가되었습니다.");
                }
                else
                {
                    Debug.Log($"플레이어 {displayName}는 이미 방 {roomName}에 존재합니다.");
                    return;
                }

                // Firebase에 수정된 데이터 저장
                string updatedJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson);

                Debug.Log($"방 {roomName} 업데이트 완료. 현재 유저 수: {roomData.UserCount}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"플레이어 추가 중 오류 발생: {ex.Message}");
            }
        }

        public async Task RemovePlayerFromRoom(string roomName, string displayName)
        {
            try
            {
                DataSnapshot snapshot = await dbReference.Child("Rooms").Child(roomName).GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogError($"방 {roomName}이 존재하지 않습니다.");
                    return;
                }

                string json = snapshot.GetRawJsonValue();
                RoomData roomData = JsonConvert.DeserializeObject<RoomData>(json);

                if (roomData.Players.ContainsKey(displayName))
                {
                    roomData.Players.Remove(displayName);
                    roomData.UserCount--;

                    if (roomData.isFull && roomData.UserCount < 10)
                        roomData.isFull = false;

                    string updatedJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                    await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson);

                    Debug.Log($"플레이어 {displayName}가 방 {roomName}에서 제거되었습니다.");
                }
                else
                    Debug.Log($"플레이어 {displayName}는 방 {roomName}에 존재하지 않습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"플레이어 제거 중 오류 발생: {ex.Message}");
            }
        }
    }
}