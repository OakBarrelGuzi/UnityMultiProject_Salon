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

    private MemoryGamePanelUi memoryGamePanelUi;
    public bool isCardFull { get; private set; } = false;

    [SerializeField]
    private Transform[] cardSpawnPos;

    private List<Card> tableCardList = new List<Card>();

    public Sprite[] cardsprite;

    [SerializeField]
    private Card cardPrefab;

    private int cardnum = 0;

    private int localPlayerScore;
    private int remotePlayerScore;

    private List<Card> openCardList = new List<Card>();

    private Coroutine turnTimeUiRoutine;
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

        // 턴 시작 시간 초기화
        turnStartTime = Time.time;

        // Firebase 리스너 등록
        roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged += OnTurnChanged;
        roomRef.Child("Board").ValueChanged += OnBoardChanged;

        UIManager.Instance.CloseAllPanels();
        UIManager.Instance.OpenPanel(PanelType.MemoryGame);
        memoryGamePanelUi = UIManager.Instance.GetComponentInChildren<MemoryGamePanelUi>();
        memoryGamePanelUi.gameObject.SetActive(true);

        turnTimeUiRoutine = StartCoroutine(TurnCountRoutine());
    }
    private void OnDestroy()
    {
        // Firebase 리스너 해제
        roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged -= OnTurnChanged;
        roomRef.Child("Board").ValueChanged -= OnBoardChanged;
    }

    private void Update()
    {
        // 턴 제한 시간 확인
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
            Debug.LogWarning("현재 플레이어의 턴이 아닙니다.");
            return;
        }

        card.cardOpen = true;
        string cardId = card.cardData.cardType.ToString();
        await roomRef.Child("Board").Child(cardId).SetRawJsonValueAsync(JsonUtility.ToJson(new CardData
        {
            IsFlipped = true,
            Owner = currentPlayerId
        }));

        // 나누름
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

        //뽑은 2개의 카드가 같을경우
        //TODO:상대턴 내턴 만들어야함
        if (openCardList[0].cardData.cardType ==
              openCardList[1].cardData.cardType)
        {
            //TODO: 점수 만들어야함         
            print("카드가 같음!");
            openCardList.Clear();
            isCardFull = false;
        }
        //뽑은 2개의 카드가 다를경우
        else
        {
            print("카드가 다름");
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
        Debug.LogWarning("턴 시간 초과. 턴을 넘깁니다.");
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
            Debug.Log($"현재 턴 플레이어: {currentPlayerId}");
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

    private IEnumerator TurnCountRoutine()
    {
        while (true)
        {

            if (currentPlayerId == GameRoomManager.Instance.currentPlayerId)
            {
                print("지금 내턴");
                memoryGamePanelUi.cardPanel.player1_Time.value = 1f - (Time.time - turnStartTime) / 60f;
                memoryGamePanelUi.cardPanel.player2_Time.value = 1f;

            }
            else if (currentPlayerId != GameRoomManager.Instance.currentPlayerId)
            {
                print("상대 턴");
                memoryGamePanelUi.cardPanel.player2_Time.value = 1f - (Time.time - turnStartTime) / 60f;
                memoryGamePanelUi.cardPanel.player1_Time.value = 1f;
            }

            yield return new WaitForSeconds(0.5f);

        }

    }
}
