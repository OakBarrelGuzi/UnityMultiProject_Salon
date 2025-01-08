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

[System.Serializable]
public class RoomData
{
    public Dictionary<string, MessageData> Messages { get; set; }
    public Dictionary<string, PlayerData> Players { get; set; }
    public int UserCount;
    public bool isFull;

    public RoomData()
    {
        Messages = new Dictionary<string, MessageData>
        {
            { "welcome", new MessageData("system", "Welcome to the room!", DateTimeOffset.UtcNow.ToUnixTimeSeconds()) }
        };
        Players = null;
        UserCount = 0;
        isFull = false;
    }
    public bool AddPlayer(PlayerData player)
    {
        if (true == isFull)
            return false;

        if (false == Players.ContainsKey(player.PlayerId))
        {
            Players[player.PlayerId] = player;
            UserCount++;

            if (UserCount >= 10)
                isFull = true;
            return true;
        }
        return false;
    }
}

[System.Serializable]
public class MessageData
{
    public string SenderId { get; set; }
    public string Content { get; set; }
    public long Timestamp { get; set; }

    public MessageData(string senderId, string content, long timestamp)
    {
        SenderId = senderId;
        Content = content;
        Timestamp = timestamp;
    }
}

[System.Serializable]
public class PlayerData
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public bool IsOnline { get; set; }

    public PlayerData(string playerId, string playerName, bool isOnline)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        IsOnline = isOnline;
    }
}

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

    private void InitializeFirebase()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        ExistRooms();
    }

    private void ExistRooms()
    {
        Debug.Log("Firebase 방 생성 시작");

        // Firebase에서 현재 존재하는 방 목록 확인
        dbReference.Child("Rooms").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // 이미 존재하는 방 이름 저장
                HashSet<string> existingRooms = new HashSet<string>();
                foreach (DataSnapshot room in snapshot.Children)
                {
                    existingRooms.Add(room.Key);
                }

                CreateMissingRooms(existingRooms);
            }
            else
                Debug.LogError("방 데이터 확인 실패: " + task.Exception);
        });
    }

    private void CreateMissingRooms(HashSet<string> existingRooms)
    {
        Debug.Log("Firebase 나머지 방 생성 시작");
        Dictionary<string, RoomData> rooms = new Dictionary<string, RoomData>();

        for (int i = 1; i <= 10; i++)
        {
            string roomName = $"Room{i}";

            if (!existingRooms.Contains(roomName))
            {
                RoomData roomData = new RoomData();
                rooms[roomName] = roomData;
            }
        }

        if (rooms.Count > 0)
        {
            string jsonData = JsonConvert.SerializeObject(rooms, Formatting.Indented);
            Debug.Log($"직렬화된 JSON 데이터: {jsonData}");
            Debug.Log($"JSON 데이터 크기: {jsonData.Length} bytes");

            dbReference.Child("Rooms").SetRawJsonValueAsync(jsonData).ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Debug.LogError("방 생성 실패: " + task.Exception);
                else if (task.IsCompleted)
                    Debug.Log("누락된 방 생성 완료!");
            });
        }
        else
        {
            Debug.Log("모든 방이 이미 존재합니다. 추가 작업이 필요하지 않습니다.");
            TestAddPlayersToRoom();
        }
    }
    public void AddPlayerToRoom(string roomName, string playerId, string playerName)
    {
        dbReference.Child("Rooms").Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            try {
            if (task.IsFaulted)
            {
                Debug.LogError($"방 {roomName} 데이터 가져오기 실패: {task.Exception}");
                return;
            }

                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (!snapshot.Exists)
                    {
                        Debug.LogError($"방 {roomName}이 존재하지 않습니다.");
                        return;
                    }

                    string json = snapshot.GetRawJsonValue();
                    RoomData roomData = JsonConvert.DeserializeObject<RoomData>(json);

                    if (roomData.isFull)
                    {
                        Debug.Log($"방 {roomName}은 이미 가득 찼습니다. 플레이어 추가 불가.");
                        return;
                    }

                    PlayerData newPlayer = new PlayerData(playerId, playerName, true);
                    bool added = roomData.AddPlayer(newPlayer);

                    if (added)
                    {
                        string updatedJson = JsonConvert.SerializeObject(roomData, Formatting.Indented);
                        dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson).ContinueWithOnMainThread(updateTask =>
                        {
                            try
                            {
                                if (updateTask.IsFaulted)
                                    Debug.LogError($"플레이어 추가 실패: {updateTask.Exception}");
                                else if (updateTask.IsCompleted)
                                    Debug.Log($"플레이어 {playerName}가 방 {roomName}에 추가되었습니다.");
                            }
                            catch (Exception ex)
                            {
                                Debug.Log(ex.Message);
                            }
                        });
                    }
                    else
                        Debug.Log($"플레이어 {playerName} 추가 실패: 이미 존재하거나 방이 가득 찼습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        });
    }
    public void RemovePlayerFromRoom(string roomName, string playerId)
    {
        dbReference.Child("Rooms").Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"방 {roomName} 데이터 가져오기 실패: {task.Exception}");
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
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
                    dbReference.Child("Rooms").Child(roomName).SetRawJsonValueAsync(updatedJson).ContinueWithOnMainThread(updateTask =>
                    {
                        if (updateTask.IsFaulted)
                            Debug.LogError($"플레이어 제거 실패: {updateTask.Exception}");
                        else if (updateTask.IsCompleted)
                            Debug.Log($"플레이어 {playerId}가 방 {roomName}에서 제거되었습니다.");
                    });
                }
                else
                    Debug.Log($"플레이어 {playerId}는 방 {roomName}에 존재하지 않습니다.");
            }
        });
    }
    void TestAddPlayersToRoom()
    {
        string roomName = "Room1";

        // 10명의 플레이어를 생성하여 추가
        for (int i = 1; i <= 10; i++)
        {
            string playerId = $"player{i}";
            string playerName = $"Player{i}";

            AddPlayerToRoom(roomName, playerId, playerName);
        }

        // 방 데이터 확인
        dbReference.Child("Rooms").Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"방 {roomName} 데이터 가져오기 실패: {task.Exception}");
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

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
        });
    }
}