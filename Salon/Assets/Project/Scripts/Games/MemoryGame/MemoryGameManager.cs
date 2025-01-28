using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Salon.Firebase;
using Salon.Firebase.Database;
using Newtonsoft.Json;
using Random = UnityEngine.Random;
using System.Threading.Tasks;
using System;
using System.Runtime.CompilerServices;
using Salon.Character;
using Character;
using System.IO;

public class MemoryGameManager : MonoBehaviour
{
    private const int CARDCOUNT = 14;
    private const int TURN_TIME_LIMIT = 60;

    private MemoryGamePanelUi memoryGamePanelUi;
    public bool isCardFull { get; private set; } = false;

    [SerializeField]
    private Transform[] cardSpawnPos;

    private List<Card> tableCardList = new List<Card>();
    private Dictionary<string, CardData> board = new Dictionary<string, CardData>();

    public Sprite[] cardsprite;

    [SerializeField]
    private Card cardPrefab;

    private int cardnum = 0;

    private bool isAnimating = false;

    private List<Card> openCardList = new List<Card>();

    private Coroutine turnTimeUiRoutine;
    private string currentPlayerId;
    private string roomId;
    private DatabaseReference roomRef;

    public string UserUID { get; private set; }
    public DatabaseReference currentUserRef { get; private set; }
    public int myGold { get; private set; } = 0;

    public LocalPlayer localPlayer;
    public RemotePlayer remotePlayer;

    private float turnStartTime;
    private void Start()
    {
        roomId = GameRoomManager.Instance.currentRoomId;
        roomRef = GameRoomManager.Instance.roomRef;
        UserUID = FirebaseManager.Instance.CurrentUserUID;
        currentUserRef = FirebaseManager.Instance.DbReference.Child("Users").Child(UserUID).Child("Gold");

        turnStartTime = Time.time;

        CardRandomSet();
        MyGoldLoad();

        roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged += OnTurnChanged;
        roomRef.Child("Board").ValueChanged += OnBoardChanged;
        roomRef.Child("Players").ValueChanged += OnPlayersDataChanged;

        GetCustomizationData();

        UIManager.Instance.CloseAllPanels();
        UIManager.Instance.OpenPanel(PanelType.MemoryGame);
        memoryGamePanelUi = UIManager.Instance.GetComponentInChildren<MemoryGamePanelUi>();
        memoryGamePanelUi.gameObject.SetActive(true);

        turnTimeUiRoutine = StartCoroutine(TurnCountRoutine());
    }
    private async void MyGoldLoad()
    {
        try
        {
            var snapshot = await currentUserRef.GetValueAsync();
            if (snapshot.Exists)
            {
                myGold = int.Parse(snapshot.Value.ToString());
                print($"돈가져왔당! {myGold}");
            }
            else
            {
                Debug.Log("점수 데이터가 없습니다");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"점수 가져오기 실패: {e.Message}");
        }
    }
    public async void MyGoldWrite(int Gold)
    {
        myGold += Gold;
        await currentUserRef.SetValueAsync(myGold);
    }

    public async void GetCustomizationData()
    {
        try
        {
            // 로컬 플레이어의 커스터마이제이션 데이터 불러오기
            var localCustomRef = FirebaseManager.Instance.DbReference
                .Child("Users")
                .Child(FirebaseManager.Instance.CurrentUserUID)
                .Child("CharacterCustomization");

            var localSnapshot = await localCustomRef.GetValueAsync();
            if (localSnapshot.Exists)
            {
                var localCustomData = JsonConvert.DeserializeObject<CharacterCustomizationData>(localSnapshot.GetRawJsonValue());
                var localCM = localPlayer.GetComponent<CharacterCustomizationManager>();
                localCM.ApplyCustomizationData(localCustomData.selectedOptions);
            }

            // 원격 플레이어의 UID 가져오기
            var playersSnapshot = await roomRef.Child("Players").GetValueAsync();
            string remotePlayerUID = null;

            foreach (var player in playersSnapshot.Children)
            {
                if (player.Key != GameRoomManager.Instance.currentPlayerId)
                {
                    remotePlayerUID = await FirebaseManager.Instance.GetUIDByDisplayName(player.Key);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(remotePlayerUID))
            {
                // 원격 플레이어의 커스터마이제이션 데이터 불러오기
                var remoteCustomRef = FirebaseManager.Instance.DbReference
                    .Child("Users")
                    .Child(remotePlayerUID)
                    .Child("CharacterCustomization");

                var remoteSnapshot = await remoteCustomRef.GetValueAsync();
                if (remoteSnapshot.Exists)
                {
                    var remoteCustomData = JsonConvert.DeserializeObject<CharacterCustomizationData>(remoteSnapshot.GetRawJsonValue());
                    var remoteCM = remotePlayer.GetComponent<CharacterCustomizationManager>();
                    remoteCM.ApplyCustomizationData(remoteCustomData.selectedOptions);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"커스터마이제이션 데이터 로드 중 오류 발생: {ex.Message}");
        }
    }
    private void OnDestroy()
    {
        // Firebase 리스너 해제
        roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged -= OnTurnChanged;
        roomRef.Child("Board").ValueChanged -= OnBoardChanged;
        roomRef.Child("Players").ValueChanged -= OnPlayersDataChanged;
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
    private async void CardRandomSet()
    {
        bool isHost = await IsHost();
        int cardIndexNumber = 0;

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
                cardType = (CARDTYPE)(cardnum % 7),
                cardSprite = cardsprite[cardnum % 7],
                cardIndex = cardIndexNumber
            };
            card.Initialize(this);
            tableCardList.Add(card);

            string cardId = card.cardData.cardIndex.ToString();
            if (isHost)
            {
                CardData cardData = new CardData { IsFlipped = false };
                board[cardId] = cardData;
                try
                {
                    await roomRef.Child("Board").Child(cardId).SetRawJsonValueAsync(JsonConvert.SerializeObject(cardData));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Board 노드 생성 실패: {ex.Message}");
                }
            }
            cardnum++;
            cardIndexNumber++;
        }
    }

    public async void CardOpen(Card card)
    {
        if (currentPlayerId != GameRoomManager.Instance.currentPlayerId)
        {
            Debug.LogWarning("현재 플레이어의 턴이 아닙니다.");
            return;
        }

        string cardId = card.cardData.cardIndex.ToString();
        var cardSnapshot = await roomRef.Child("Board").Child(cardId).GetValueAsync();

        if (!cardSnapshot.Exists)
        {
            Debug.LogError($"[CardOpen] Firebase에서 {cardId}를 찾을 수 없습니다.");
            return;
        }

        var cardData = JsonConvert.DeserializeObject<CardData>(cardSnapshot.GetRawJsonValue());
        if (cardData.IsFlipped)
        {
            Debug.LogWarning("이미 뒤집힌 카드입니다.");
            return;
        }

        card.cardOpen = true;
        cardData.IsFlipped = true;
        board[cardId] = cardData;

        try
        {
            await roomRef.Child("Board").Child(cardId).SetRawJsonValueAsync(JsonConvert.SerializeObject(cardData));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CardOpen] Firebase 업데이트 실패: {ex.Message}");
        }

        // 나누름
        openCardList.Add(card);
        StartCoroutine(TurnRoutine(card));
        if (openCardList.Count >= 2)
        {
            isCardFull = true;
            StartCoroutine(CardCheckRoutine());
        }

    }
    private bool AreAllCardsOpen()
    {
        foreach (var card in tableCardList)
        {
            if (!card.cardOpen)
            {
                return false;
            }
        }
        return true;
    }
    private void GameEnd()
    {
        memoryGamePanelUi.cardResultUi.gameObject.SetActive(true);
        int localScore = int.Parse(memoryGamePanelUi.cardPanel.localPlayerScore.text);
        int remoteScore = int.Parse(memoryGamePanelUi.cardPanel.remotePlayerScore.text);
        string localName = memoryGamePanelUi.cardPanel.localPlayerName.text;
        string remoteName = memoryGamePanelUi.cardPanel.remotePlayerName.text;

        memoryGamePanelUi.cardResultUi.localPlayerScore.text = localScore.ToString();
        memoryGamePanelUi.cardResultUi.localPlayerName.text = localName;
        memoryGamePanelUi.cardResultUi.remotePlayerScore.text = remoteScore.ToString();
        memoryGamePanelUi.cardResultUi.remotePlayerName.text = remoteName;
        memoryGamePanelUi.cardResultUi.MyGold.text = this.myGold.ToString();

        if (localScore > remoteScore)
        {
            MyGoldWrite(20);
        }
        memoryGamePanelUi.cardPanel.gameObject.SetActive(false);
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

            Task updateTask = UpdatePlayerScoreAsync(GameRoomManager.Instance.currentPlayerId, 1);
            MyGoldWrite(5);
            yield return new WaitUntil(() => updateTask.IsCompleted);

            if (AreAllCardsOpen())
            {
                GameEnd();
            }
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
        isAnimating = true;

        foreach (var card in openCardList)
        {
            string cardId = card.cardData.cardIndex.ToString();
            if (!board[cardId].IsFlipped) continue;

            board[cardId].IsFlipped = false;
            card.cardOpen = false;

            roomRef.Child("Board").Child(cardId).SetRawJsonValueAsync(JsonConvert.SerializeObject(board[cardId]));
        }

        // 카드 뒤집기 애니메이션 실행
        yield return StartCoroutine(TurnRoutine(openCardList[1]));
        yield return StartCoroutine(TurnRoutine(openCardList[0]));

        openCardList.Clear();
        isCardFull = false;

        isAnimating = false;
        UpdateTurnToNextPlayer();
    }

    public IEnumerator TurnRoutine(Card card)
    {
        float elapsedTime = 0f;
        //card.cardOpen = !card.cardOpen;

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
        if (isAnimating || !e.Snapshot.Exists)
        {
            return;
        }

        foreach (var child in e.Snapshot.Children)
        {
            var cardData = JsonConvert.DeserializeObject<CardData>(child.GetRawJsonValue());
            string cardId = child.Key;
            board[cardId] = cardData;

            var card = tableCardList.Find(c => c.cardData.cardIndex.ToString() == cardId);
            if (card != null && !card.isTurning)
            {
                if (cardData.IsFlipped && !card.cardOpen)
                {
                    card.cardOpen = true;
                    StartCoroutine(TurnRoutine(card));
                }
                else if (!cardData.IsFlipped && card.cardOpen)
                {
                    card.cardOpen = false;
                    StartCoroutine(TurnRoutine(card));
                }
            }
        }

        if (AreAllCardsOpen())
        {
            GameEnd();
        }
    }

    private IEnumerator TurnCountRoutine()
    {
        while (true)
        {

            if (currentPlayerId == GameRoomManager.Instance.currentPlayerId)
            {
                memoryGamePanelUi.cardPanel.localPlayerTime.value = 1f - (Time.time - turnStartTime) / 60f;
                memoryGamePanelUi.cardPanel.remotePlayerTime.value = 1f;

            }
            else if (currentPlayerId != GameRoomManager.Instance.currentPlayerId)
            {
                memoryGamePanelUi.cardPanel.remotePlayerTime.value = 1f - (Time.time - turnStartTime) / 60f;
                memoryGamePanelUi.cardPanel.localPlayerTime.value = 1f;
            }

            yield return new WaitForSeconds(0.5f);

        }

    }
    private async Task<bool> IsHost()
    {
        var snapshot = await roomRef.GetValueAsync();
        if (snapshot.Exists)
        {
            var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
            if (roomData.Players.ContainsKey(GameRoomManager.Instance.currentPlayerId))
            {
                return roomData.Players[GameRoomManager.Instance.currentPlayerId].IsHost;
            }
        }
        Debug.LogWarning("호스트 여부를 확인할 수 없습니다.");
        return false;
    }
    private async Task UpdatePlayerScoreAsync(string playerId, int scoreToAdd)
    {
        try
        {
            var playerRef = roomRef.Child("Players").Child(playerId).Child("Score");
            var snapshot = await playerRef.GetValueAsync();

            int currentScore = snapshot.Exists ? int.Parse(snapshot.Value.ToString()) : 0;
            int newScore = currentScore + scoreToAdd;

            await playerRef.SetValueAsync(newScore);
        }
        catch (Exception ex)
        {
            Debug.LogError($"플레이어 점수 업데이트 중 오류 발생: {ex.Message}");
        }
    }
    private void OnPlayersDataChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.Snapshot.Exists)
        {
            foreach (var child in e.Snapshot.Children)
            {
                string playerId = child.Key;
                var playerData = JsonConvert.DeserializeObject<PlayerData>(child.GetRawJsonValue());

                if (playerId == GameRoomManager.Instance.currentPlayerId)
                {
                    memoryGamePanelUi.cardPanel.localPlayerName.text = playerData.DisplayName;
                    memoryGamePanelUi.cardPanel.localPlayerScore.text = playerData.Score.ToString();
                }
                else
                {
                    memoryGamePanelUi.cardPanel.remotePlayerName.text = playerData.DisplayName;
                    memoryGamePanelUi.cardPanel.remotePlayerScore.text = playerData.Score.ToString();
                }
            }
        }
    }
}
