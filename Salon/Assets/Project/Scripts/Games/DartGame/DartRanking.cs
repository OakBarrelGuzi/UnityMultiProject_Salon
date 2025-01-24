using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Salon.Firebase;
using Salon.Firebase.Database;
using System.Linq;

public class DartRanking : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] rankerIdText;
    [SerializeField] private TextMeshProUGUI[] rankerScoreText;
    [SerializeField] private TextMeshProUGUI playerRank;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private TextMeshProUGUI playerScoreText;

    private DatabaseReference dbRef;
    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("Users");
        LoadRankingData();
    }
    private async void LoadRankingData()
    {
        try
        {
            var snapshot = await dbRef.GetValueAsync();

            if (snapshot.Exists)
            {
                //모든 유저 저장
                List<RankingData> userList = new List<RankingData>();

                foreach (var child in snapshot.Children)
                {
                    string userId = child.Key;
                    var data = JsonUtility.FromJson<UserData>(child.GetRawJsonValue());
                    userList.Add(new RankingData() { 
                        DisplayName = data.DisplayName,
                        BestDartScore = data.BestDartScore
                    });
                }

                //점수 기준 내림차순 정렬
                userList = userList.OrderByDescending(user => user.BestDartScore).ToList();

                //랭킹 UI에 상위 5명 표시
                DisplayTopRanking(userList);

                string currentUserId = FirebaseManager.Instance.CurrentUserUID;
                var currentUserName = await dbRef.Child(currentUserId).GetValueAsync();
                DisplayPlayerRank(userList, currentUserName);
            }
            else
            {
                Debug.Log("랭킹 데이터를 가져올 수 없습니다.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"랭킹 데이터를 가져오는 중 오류 발생: {ex.Message}");
        }
    }
    private void DisplayTopRanking(List<RankingData> userList)
    {
        for (int i = 0; i < Mathf.Min(5, userList.Count); i++)
        {
            rankerIdText[i].text = userList[i].DisplayName;
            rankerScoreText[i].text = userList[i].BestDartScore.ToString();
        }
    }
    private void DisplayPlayerRank(List<RankingData> userList, DataSnapshot currentUserName)
    {
        playerRank.text = (userList.FindIndex(user => user.DisplayName == currentUserName.Child("DisplayName").GetRawJsonValue()) + 1).ToString();

        playerIdText.text = currentUserName.Child("DisplayName").GetRawJsonValue();
        playerScoreText.text = currentUserName.Child("BestDartScore").GetRawJsonValue();
    }
}

public class RankingData
{
    public string DisplayName;
    public int BestDartScore;
}
