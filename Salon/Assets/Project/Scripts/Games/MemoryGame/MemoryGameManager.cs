using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Salon.Firebase;
using Salon.Firebase.Database;
using Newtonsoft.Json;


public class MemoryGameManager : MonoBehaviour
{
    private const int CARDCOUNT = 14;
    private const int TURN_TIME_LIMIT = 60;

    public bool isCardFull { get; private set; } = false;

    [SerializeField]
    private Transform[] cardSpawnPos;
        
    private List<Card> tableCardList = new List<Card>();

    public Sprite[] cardsprite;

    [SerializeField]
    private Card cardPrefab;

    private int cardnum = 0;

    private List<Card> openCardList = new List<Card>();

    private string currentPlayerId;
    private string roomId;
    private DatabaseReference roomRef;

    private float turnStartTime;
    private void Start()
    {
        CardRandomSet();

        roomId = GameRoomManager.Instance.currentRoomId;
        roomRef = FirebaseDatabase.DefaultInstance
            .GetReference("Channels")
            .Child(GameRoomManager.Instance.currentChannelId)
            .Child("GameRooms")
            .Child(roomId);

        // �� ���� �ð� �ʱ�ȭ
        turnStartTime = Time.time;

        // Firebase ������ ���
        roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged += OnTurnChanged;
        roomRef.Child("Board").ValueChanged += OnBoardChanged;
    }
    private void OnDestroy()
    {
        // Firebase ������ ����
        roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged -= OnTurnChanged;
        roomRef.Child("Board").ValueChanged -= OnBoardChanged;
    }

    private void Update()
    {
        // �� ���� �ð� Ȯ��
        if (currentPlayerId == GameRoomManager.Instance.currentPlayerId &&
            Time.time - turnStartTime > TURN_TIME_LIMIT)
        {
            SkipTurn();
        }
    }
    private void CardRandomSet()
    {
        HashSet<int> randomCardSet = new HashSet<int>();
        while (randomCardSet.Count < CARDCOUNT)
        {
            randomCardSet.Add(Random.Range(0, cardSpawnPos.Length));
        }

        foreach (int i in randomCardSet)
        {
            Card card = Instantiate(cardPrefab, cardSpawnPos[i]);

            card.cardData = new InGameCard
            {
                cardType = (CARDTYPE)cardnum,
                cardSprite = cardsprite[cardnum],
            };
            card.Initialize(this);
            tableCardList.Add(card);
            cardnum++;
            if (cardnum > 6) cardnum = 0;
        }
    }

    public async void CardOpen(Card card)
    {
        if (currentPlayerId != GameRoomManager.Instance.currentPlayerId)
        {
            Debug.LogWarning("���� �÷��̾��� ���� �ƴմϴ�.");
            return;
        }

        card.cardOpen = true;
        string cardId = card.cardData.cardType.ToString();
        await roomRef.Child("Board").Child(cardId).SetRawJsonValueAsync(JsonUtility.ToJson(new CardData
        {
            IsFlipped = true,
            Owner = currentPlayerId
        }));

        // ������
        openCardList.Add(card);
        StartCoroutine(TurnRoutine(card));
        if (openCardList.Count >= 2)
        {
            isCardFull = true;
            StartCoroutine(CardCheckRoutine());
        }

    }
    private IEnumerator CardCheckRoutine()
    {
        yield return new WaitUntil(() => !openCardList[1].isTurning);

        yield return new WaitForSeconds(0.5f);

        //���� 2���� ī�尡 �������
        //TODO:����� ���� ��������
        if (openCardList[0].cardData.cardType ==
              openCardList[1].cardData.cardType)
        {
            //TODO: ���� ��������         
            print("ī�尡 ����!");
            openCardList.Clear();
            isCardFull = false;
        }
        //���� 2���� ī�尡 �ٸ����
        else
        {
            print("ī�尡 �ٸ�");
            StartCoroutine(FailRoutine());
        }
    }
    public IEnumerator FailRoutine()
    {
        yield return StartCoroutine(TurnRoutine(openCardList[1]));

        yield return StartCoroutine(TurnRoutine(openCardList[0]));

        openCardList.Clear();
        isCardFull = false;
    }

    public IEnumerator TurnRoutine(Card card)
    {
        float elapsedTime = 0f;
        card.cardOpen = !card.cardOpen;

        card.isTurning = true;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime;

            card.transform.localRotation =
                Quaternion.Slerp(card.startQuaternion,
                card.targetQuaternion,
                elapsedTime * card.turnSpeed);
            yield return null;
        }
        Quaternion temp = card.startQuaternion;
        card.startQuaternion = card.targetQuaternion;
        card.targetQuaternion = temp;

        card.isTurning = false;
    }
    private void SkipTurn()
    {
        Debug.LogWarning("�� �ð� �ʰ�. ���� �ѱ�ϴ�.");
        UpdateTurnToNextPlayer();
    }
    private async void UpdateTurnToNextPlayer()
    {
        var snapshot = await roomRef.GetValueAsync();
        if (!snapshot.Exists) return;

        var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
        var players = new List<string>(roomData.Players.Keys);
        int currentIndex = players.IndexOf(currentPlayerId);

        string nextPlayerId = players[(currentIndex + 1) % players.Count];
        await roomRef.Child("GameState").Child("CurrentTurnPlayerId").SetValueAsync(nextPlayerId);
    }
    private void OnTurnChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.Snapshot.Exists)
        {
            currentPlayerId = e.Snapshot.Value.ToString();
            turnStartTime = Time.time;
            Debug.Log($"���� �� �÷��̾�: {currentPlayerId}");
        }
    }
    private void OnBoardChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.Snapshot.Exists)
        {
            foreach (var child in e.Snapshot.Children)
            {
                var cardData = JsonUtility.FromJson<CardData>(child.GetRawJsonValue());
                var card = tableCardList.Find(c => c.cardData.cardType.ToString() == child.Key);

                if (card != null && cardData.IsFlipped && !card.cardOpen)
                {
                    StartCoroutine(TurnRoutine(card));
                }
            }
        }
    }
}
