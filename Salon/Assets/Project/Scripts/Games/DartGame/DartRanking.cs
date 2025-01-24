using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Salon.Firebase;
using Salon.Firebase.Database;
using System.Linq;
using UnityEngine.UI;

public class DartRanking : Panel
{
    [SerializeField] private TextMeshProUGUI[] rankerIdText;
    [SerializeField] private TextMeshProUGUI[] rankerScoreText;
    [SerializeField] private TextMeshProUGUI playerRank;
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private Button closeButton;

    private DatabaseReference dbRef;

    private void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("Users");
    }

    public override void Open()
    {
        base.Open();
        closeButton.onClick.AddListener(CloseButtonClick);
        LoadRankingData();
    }

    public override void Close()
    {
        closeButton.onClick.RemoveAllListeners();
        base.Close();
    }

    private void CloseButtonClick()
    {
        UIManager.Instance.ClosePanel(PanelType.DartRanking);
        UIManager.Instance.OpenPanel(PanelType.DartGame);
    }

    private async void LoadRankingData()
    {
        try
        {
            var snapshot = await dbRef.GetValueAsync();

            if (snapshot.Exists)
            {
                //��� ���� ����
                List<RankingData> userList = new List<RankingData>();

                foreach (var child in snapshot.Children)
                {
                    var displayNameSnapshot = child.Child("DisplayName");
                    var bestDartScoreSnapshot = child.Child("BestDartScore");

                    if (displayNameSnapshot.Exists && bestDartScoreSnapshot.Exists)
                    {
                        userList.Add(new RankingData()
                        {
                            DisplayName = displayNameSnapshot.Value.ToString(),
                            BestDartScore = int.Parse(bestDartScoreSnapshot.Value.ToString())
                        });
                    }
                }
                userList = userList.OrderByDescending(user => user.BestDartScore).ToList();

                DisplayTopRanking(userList);

                string currentUserId = FirebaseManager.Instance.CurrentUserUID;
                DataSnapshot currentUserName = await dbRef.Child(currentUserId).GetValueAsync();
                DisplayPlayerRank(userList, currentUserName);
            }
            else
            {
                Debug.Log("��ŷ �����͸� ������ �� �����ϴ�.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"데이터 가져오기 실패: {ex.Message}");
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
    private void DisplayPlayerRank(List<RankingData> userList, DataSnapshot currentUserSnapshot)
    {
        string currentUserDisplayName = currentUserSnapshot.Child("DisplayName").Value.ToString();
        string currentUserBestScore = currentUserSnapshot.Child("BestDartScore").Value.ToString();

        int rankIndex = userList.FindIndex(user => user.DisplayName == currentUserDisplayName);
        playerRank.text = (rankIndex + 1).ToString();

        playerIdText.text = currentUserDisplayName;
        playerScoreText.text = currentUserBestScore;
    }
}

public class RankingData
{
    public string DisplayName;
    public int BestDartScore;
}
