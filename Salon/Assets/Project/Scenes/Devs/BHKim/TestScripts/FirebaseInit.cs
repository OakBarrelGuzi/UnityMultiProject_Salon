using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.InteropServices;
using Firebase.Extensions;
using Salon.Firebase.Database;
using System.Threading.Tasks;

public class FirebaseInit : MonoBehaviour
{
    private DatabaseReference dbReference;
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log($"Firebase 초기화 성공");
                InitializeFirebase();
            }
            else
                Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
        });
    }

    private async void InitializeFirebase()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        await ExistRooms();
        await InitPlayerMappings();
    }

    private async Task InitPlayerMappings()
    {
        try
        {
            DataSnapshot roomsSnapshot = await dbReference.Child("Rooms").GetValueAsync();

            if (!roomsSnapshot.Exists)
            {
                Debug.LogWarning("Rooms 데이터가 없습니다. 매핑을 생성하지 않습니다.");
                return;
            }

            DataSnapshot mappingSnapshot = await dbReference.Child("PlayerNameToIdMapping").GetValueAsync();

            HashSet<string> existingNames = new HashSet<string>();
            if (mappingSnapshot.Exists)
            {
                foreach (DataSnapshot entry in mappingSnapshot.Children)
                {
                    existingNames.Add(entry.Key);
                }
            }

            List<Task> mappingTasks = new List<Task>();
            foreach (DataSnapshot roomSnapshot in roomsSnapshot.Children)
            {
                DataSnapshot playersSnapshot = roomSnapshot.Child("Players");

                if (playersSnapshot.Exists)
                {
                    foreach (DataSnapshot playerSnapshot in playersSnapshot.Children)
                    {
                        string playerId = playerSnapshot.Key; // Player ID
                        string playerName = playerSnapshot.Child("PlayerName").Value?.ToString(); // Player Name

                        if (!string.IsNullOrEmpty(playerName) && !existingNames.Contains(playerName))
                        {
                            mappingTasks.Add(Task.Run(async () =>
                            {
                                try
                                {
                                    await dbReference.Child("PlayerNameToIdMapping").Child(playerName).SetValueAsync(playerId);
                                    Debug.Log($"플레이어 이름 {playerName}와 ID {playerId} 매핑 완료");
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"플레이어 {playerName} 매핑 중 오류 발생: {ex.Message}");
                                }
                            }));
                        }
                    }
                }
            }

            // 모든 매핑 작업이 완료될 때까지 대기
            await Task.WhenAll(mappingTasks);

            Debug.Log("Firebase 이름-아이디 매핑 초기화 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"이름-아이디 매핑 초기화 중 오류 발생: {ex.Message}");
        }
    }
    public async Task<UserMapping> GetPlayerMappingByName(string playerName)
    {
        try
        {
            DataSnapshot snapshot = await dbReference.Child("PlayerNameToIdMapping").Child(playerName).GetValueAsync();

            if (snapshot.Exists)
            {
                string playerId = snapshot.Value.ToString();
                return new UserMapping(playerId, playerName);
            }
            else
            {
                Debug.LogError($"플레이어 이름 {playerName}을 찾을 수 없습니다.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ID 조회 중 오류 발생: {ex.Message}");
            return null;
        }
    }
    public async Task InvitePlayerToRoomByName(string roomName, string playerName)
    {
        UserMapping mapping = await GetPlayerMappingByName(playerName);

        if (mapping != null)
        {
            await AddPlayerToRoom(roomName, mapping.userId, mapping.userName);
            Debug.Log($"플레이어 {playerName}가 방 {roomName}으로 초대되었습니다.");
        }
        else
        {
            Debug.LogError($"플레이어 {playerName} 초대 실패: 이름을 찾을 수 없습니다.");
        }
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

        // 테스트 코드
        await AddPlayerToRoom("Room1", "player1", "player1");
        await TestAddPlayersToRoom();
    }
    public async Task AddPlayerToRoom(string roomName, string playerId, string playerName)
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
            if (false == roomData.Players.ContainsKey(playerId))
            {
                GamePlayerData newPlayer = new GamePlayerData(playerId, playerName, true);
                roomData.Players[playerId] = newPlayer;
                roomData.UserCount++;

                // 방이 가득 찼는지 확인
                if (roomData.UserCount >= 10)
                    roomData.isFull = true;

                Debug.Log($"플레이어 {playerName}가 방 {roomName}에 추가되었습니다.");
            }
            else
            {
                Debug.Log($"플레이어 {playerName}는 이미 방 {roomName}에 존재합니다.");
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

    public async Task RemovePlayerFromRoom(string roomName, string playerId)
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

            if (roomData.Players.ContainsKey(playerId))
            {
                roomData.Players.Remove(playerId);
                roomData.UserCount--;

                if (roomData.isFull && roomData.UserCount < 10)
                    roomData.isFull = false;

                string updatedJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                await dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson);

                Debug.Log($"플레이어 {playerId}가 방 {roomName}에서 제거되었습니다.");
            }
            else
                Debug.Log($"플레이어 {playerId}는 방 {roomName}에 존재하지 않습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"플레이어 제거 중 오류 발생: {ex.Message}");
        }
    }

    public async Task TestAddPlayersToRoom()
    {
        string roomName = "Room1";

        // 10명의 플레이어를 생성하여 추가
        for (int i = 1; i <= 10; i++)
        {
            string playerId = $"player{i}";
            string playerName = $"Player{i}";

            await AddPlayerToRoom(roomName, playerId, playerName);
        }

        // 방 데이터 확인
        try
        {
            DataSnapshot snapshot = await dbReference.Child("Rooms").Child(roomName).GetValueAsync();

            if (snapshot.Exists)
            {
                string json = snapshot.GetRawJsonValue();
                RoomData roomData = JsonConvert.DeserializeObject<RoomData>(json);

                Debug.Log($"방 이름: {roomName}");
                Debug.Log($"현재 유저 수: {roomData.UserCount}");
                Debug.Log($"방이 가득 찼는가: {roomData.isFull}");
            }
            else
            {
                Debug.LogError($"방 {roomName}이 존재하지 않습니다.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"방 상태 확인 중 오류 발생: {ex.Message}");
        }
    }
}