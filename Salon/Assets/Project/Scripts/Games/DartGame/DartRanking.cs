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
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
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
            var snapshot = await dbRef.Child("Users").GetValueAsync();

            if (snapshot.Exists)
            {
                //��� ���� ����
                List<RankingData> userList = new List<RankingData>();

                foreach (var child in snapshot.Children)
                {
                    string userId = child.Key;
                    var data = JsonUtility.FromJson<UserData>(child.GetRawJsonValue());
                    userList.Add(new RankingData()
                    {
                        DisplayName = data.DisplayName,
                        BestDartScore = data.BestDartScore
                    });
                }

                //���� ���� �������� ����
                userList = userList.OrderByDescending(user => user.BestDartScore).ToList();

                //��ŷ UI�� ���� 5�� ǥ��
                DisplayTopRanking(userList);

                string currentUserId = FirebaseManager.Instance.CurrentUserUID;
                var currentUserName = await dbRef.Child(currentUserId).GetValueAsync();
                DisplayPlayerRank(userList, currentUserName);
            }
            else
            {
                Debug.Log("��ŷ �����͸� ������ �� �����ϴ�.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"��ŷ �����͸� �������� �� ���� �߻�: {ex.Message}");
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
