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
        }
    }
}