using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System;

public class Test : MonoBehaviour
{
    private DatabaseReference dbReference;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase 연결 성공!");
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패: {dependencyStatus}");
            }
        });
    }

    private void InitializeFirebase()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        WriteTestData();
    }

    private void WriteTestData()
    {
        dbReference.Child("test").Child("message").SetValueAsync("Firebase 연결 테스트 성공!")
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("데이터 쓰기 실패: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    Debug.Log("데이터 쓰기 성공!");
                }
            });
    }
}
