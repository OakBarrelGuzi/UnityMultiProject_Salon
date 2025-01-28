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
using System.Linq;

public class MemoryGameManager : MonoBehaviour
{
    private const int CARDCOUNT = 14;
    private const int TURN_TIME_LIMIT = 60;

    private MemoryGamePanelUi memoryGamePanelUi;
    public bool isCardFull { get; private set; } = false;
    private bool isGameStarted = false;
    private bool isGameEnded = false;

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
    private async void Start()
    {
        try
        {
            roomId = GameRoomManager.Instance.currentRoomId;
            roomRef = GameRoomManager.Instance.roomRef;
            UserUID = FirebaseManager.Instance.CurrentUserUID;
            currentUserRef = FirebaseManager.Instance.DbReference.Child("Users").Child(UserUID).Child("Gold");

            turnStartTime = Time.time;

            // UI 초기화
            UIManager.Instance.CloseAllPanels();
            UIManager.Instance.OpenPanel(PanelType.MemoryGame);
            memoryGamePanelUi = UIManager.Instance.GetComponentInChildren<MemoryGamePanelUi>();

            if (memoryGamePanelUi != null)
            {
                memoryGamePanelUi.gameObject.SetActive(true);
                InitializeUI();
            }
            else
            {
                Debug.LogError("MemoryGamePanelUi를 찾을 수 없습니다.");
                return;
            }

            // 호스트 여부 로그 출력
            bool isHost = await IsHost();
            Debug.Log($"현재 플레이어 ID: {GameRoomManager.Instance.currentPlayerId}, 호스트 여부: {isHost}");

            await InitializeGame();

            turnTimeUiRoutine = StartCoroutine(TurnCountRoutine());
        }
        catch (Exception ex)
        {
            Debug.LogError($"게임 시작 중 오류 발생: {ex.Message}");
        }
    }

    private void InitializeUI()
    {
        if (memoryGamePanelUi != null && memoryGamePanelUi.cardPanel != null)
        {
            // 카드 패널 초기화
            memoryGamePanelUi.cardPanel.gameObject.SetActive(true);
            memoryGamePanelUi.cardPanel.localPlayerScore.text = "0";
            memoryGamePanelUi.cardPanel.remotePlayerScore.text = "0";
            memoryGamePanelUi.cardPanel.localPlayerTime.value = 1f;
            memoryGamePanelUi.cardPanel.remotePlayerTime.value = 1f;

            // 결과 UI 초기화
            if (memoryGamePanelUi.cardResultUi != null)
            {
                memoryGamePanelUi.cardResultUi.gameObject.SetActive(false);
                memoryGamePanelUi.cardResultUi.localPlayerScore.text = "0";
                memoryGamePanelUi.cardResultUi.remotePlayerScore.text = "0";
                memoryGamePanelUi.cardResultUi.getGoldText.text = "0";
                memoryGamePanelUi.cardResultUi.myGoldText.text = "0";
            }
        }
    }

    private async Task InitializeGame()
    {
        try
        {
            Debug.Log("게임 초기화 시작");
            isGameStarted = false;
            isGameEnded = false;
            isCardFull = false;
            isAnimating = false;

            // 이벤트 리스너 제거 및 상태 초기화
            CleanupResources();

            bool isHost = await IsHost();
            Debug.Log($"호스트 여부: {isHost}");

            // 호스트만 수행하는 초기화 작업
            if (isHost)
            {
                Debug.Log("호스트 초기화 작업 시작");
                // 게임 상태 초기화
                var updates = new Dictionary<string, object>
                {
                    ["GameState/IsEnded"] = false,
                    ["GameState/IsGameActive"] = true,
                    ["GameState/CurrentTurnPlayerId"] = GameRoomManager.Instance.currentPlayerId,
                    ["Board"] = null
                };

                // 플레이어 점수 초기화
                var playersSnapshot = await roomRef.Child("Players").GetValueAsync();
                if (playersSnapshot.Exists)
                {
                    foreach (var player in playersSnapshot.Children)
                    {
                        updates[$"Players/{player.Key}/Score"] = 0;
                    }
                }

                await roomRef.UpdateChildrenAsync(updates);
                Debug.Log("호스트 초기화 작업 완료");

                // 호스트만 카드 생성 및 배치
                CardRandomSet();
                Debug.Log("카드 생성 및 배치 완료");
            }
            else
            {
                Debug.Log("게스트 초기화 대기 시작");
                // 게스트는 호스트의 초기화가 완료될 때까지 대기
                await WaitForHostInitialization();
                Debug.Log("게스트 초기화 대기 완료");
            }

            // 공통 작업
            await MyGoldLoad();
            await GetCustomizationData();

            // 이벤트 리스너 등록
            RegisterEventListeners();

            // 게임 시작 상태 설정
            isGameStarted = true;
            Debug.Log("게임 초기화 완료");

            // 현재 턴 확인
            var turnSnapshot = await roomRef.Child("GameState").Child("CurrentTurnPlayerId").GetValueAsync();
            if (turnSnapshot.Exists)
            {
                currentPlayerId = turnSnapshot.Value.ToString();
                Debug.Log($"현재 턴 플레이어: {currentPlayerId}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"게임 초기화 중 오류 발생: {ex.Message}");
            throw;
        }
    }

    private void RegisterEventListeners()
    {
        if (roomRef != null)
        {
            roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged += OnTurnChanged;
            roomRef.Child("Board").ValueChanged += OnBoardChanged;
            roomRef.Child("Players").ValueChanged += OnPlayersDataChanged;
        }
    }

    private async Task WaitForHostInitialization()
    {
        try
        {
            // 최대 30초 동안 대기
            float waitTime = 0;
            while (waitTime < 30)
            {
                var gameStateSnapshot = await roomRef.Child("GameState").GetValueAsync();
                if (gameStateSnapshot.Exists &&
                    gameStateSnapshot.Child("IsGameActive").Exists &&
                    (bool)gameStateSnapshot.Child("IsGameActive").Value)
                {
                    return;
                }
                await Task.Delay(1000);
                waitTime += 1;
            }
            throw new TimeoutException("호스트 초기화 대기 시간 초과");
        }
        catch (Exception ex)
        {
            Debug.LogError($"호스트 초기화 대기 중 오류 발생: {ex.Message}");
            throw;
        }
    }

    private async Task MyGoldLoad()
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
    public async Task MyGoldWrite(int Gold)
    {
        try
        {
            myGold += Gold;
            await currentUserRef.SetValueAsync(myGold);
        }
        catch (Exception ex)
        {
            Debug.LogError($"골드 업데이트 실패: {ex.Message}");
        }
    }

    private async Task GetCustomizationData()
    {
        try
        {
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
        CleanupResources();
    }

    private void CleanupResources()
    {
        try
        {
            // 코루틴 정리
            if (turnTimeUiRoutine != null)
            {
                StopCoroutine(turnTimeUiRoutine);
                turnTimeUiRoutine = null;
            }

            // Firebase 이벤트 핸들러 해제
            if (roomRef != null)
            {
                roomRef.Child("GameState").Child("CurrentTurnPlayerId").ValueChanged -= OnTurnChanged;
                roomRef.Child("Board").ValueChanged -= OnBoardChanged;
                roomRef.Child("Players").ValueChanged -= OnPlayersDataChanged;
                roomRef = null;
            }

            // 게임 상태 초기화
            isGameStarted = false;
            isGameEnded = false;
            isCardFull = false;
            isAnimating = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"리소스 정리 중 오류 발생: {ex.Message}");
        }
    }

    private void Update()
    {
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
        if (tableCardList.Count != CARDCOUNT || board.Count != CARDCOUNT)
            return false;

        // 모든 카드가 뒤집혀 있는지 확인
        foreach (var cardData in board.Values)
        {
            if (!cardData.IsFlipped)
                return false;
        }

        // 실제 카드 오브젝트의 상태도 확인
        foreach (var card in tableCardList)
        {
            if (!card.cardOpen || card.isTurning)
                return false;
        }

        return true;
    }
    private async void GameEnd()
    {
        if (!isGameEnded)
        {
            isGameEnded = true;
            try
            {
                Debug.Log("게임 종료 처리 시작");
                bool isHost = await IsHost();

                // 호스트만 게임 종료 상태를 설정
                if (isHost)
                {
                    await roomRef.Child("GameState").Child("IsEnded").SetValueAsync(true);
                    await roomRef.Child("GameState").Child("IsGameActive").SetValueAsync(false);
                    Debug.Log("게임 종료 상태 설정 완료");
                }

                // UI 업데이트 (모든 플레이어)
                await UpdateGameEndUI();

                // 리소스 정리 (모든 플레이어)
                CleanupResources();

                // 호스트만 방 정리
                if (isHost)
                {
                    await CleanupGameRoom();
                }

                // 모든 플레이어 공통 - 씬 전환
                await Task.Delay(3000);
                UIManager.Instance.CloseAllPanels();
                ScenesManager.Instance.ChanageScene("LobbyScene");
                UIManager.Instance.OpenPanel(PanelType.Lobby);
            }
            catch (Exception ex)
            {
                Debug.LogError($"게임 종료 처리 중 오류 발생: {ex.Message}");
                isGameEnded = false;
            }
        }
    }

    private async Task UpdateGameEndUI()
    {
        try
        {
            if (memoryGamePanelUi != null && memoryGamePanelUi.cardResultUi != null && memoryGamePanelUi.cardPanel != null)
            {
                memoryGamePanelUi.cardResultUi.gameObject.SetActive(true);

                // 점수 정보 가져오기
                int localScore = int.Parse(memoryGamePanelUi.cardPanel.localPlayerScore.text);
                int remoteScore = int.Parse(memoryGamePanelUi.cardPanel.remotePlayerScore.text);
                string localName = memoryGamePanelUi.cardPanel.localPlayerName.text;
                string remoteName = memoryGamePanelUi.cardPanel.remotePlayerName.text;

                // UI 업데이트
                memoryGamePanelUi.cardResultUi.localPlayerScore.text = localScore.ToString();
                memoryGamePanelUi.cardResultUi.localPlayerName.text = localName;
                memoryGamePanelUi.cardResultUi.remotePlayerScore.text = remoteScore.ToString();
                memoryGamePanelUi.cardResultUi.remotePlayerName.text = remoteName;
                memoryGamePanelUi.cardResultUi.myGoldText.text = myGold.ToString();
                memoryGamePanelUi.cardResultUi.getGoldText.text = "0";

                // 승리 보상 지급
                if (localScore > remoteScore)
                {
                    memoryGamePanelUi.cardResultUi.getGoldText.text = "20";
                    await MyGoldWrite(20);
                }

                memoryGamePanelUi.cardPanel.gameObject.SetActive(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"게임 종료 UI 업데이트 중 오류 발생: {ex.Message}");
            throw;
        }
    }

    private async Task CleanupGameRoom()
    {
        try
        {
            Debug.Log("게임룸 정리 시작");
            var gameRoomsRef = FirebaseManager.Instance.DbReference
                .Child("Channels")
                .Child(GameRoomManager.Instance.currentChannelId)
                .Child("GameRooms");

            var snapshot = await gameRoomsRef.GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var room in snapshot.Children)
                {
                    var roomData = JsonConvert.DeserializeObject<GameRoomData>(room.GetRawJsonValue());
                    if (roomData != null && roomData.HostPlayerId == GameRoomManager.Instance.currentPlayerId)
                    {
                        await gameRoomsRef.Child(room.Key).RemoveValueAsync();
                        Debug.Log($"방 삭제 완료: {room.Key}");
                    }
                }
            }
            Debug.Log("게임룸 정리 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"게임룸 정리 중 오류 발생: {ex.Message}");
            throw;
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

            Task updateTask = UpdatePlayerScoreAsync(GameRoomManager.Instance.currentPlayerId, 1);
            Task goldTask = MyGoldWrite(5);
            yield return new WaitUntil(() => updateTask.IsCompleted && goldTask.IsCompleted);

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
        if (!this.gameObject.activeInHierarchy || !isGameStarted || isGameEnded || isAnimating)
        {
            return;
        }

        if (!e.Snapshot.Exists || tableCardList.Count == 0)
        {
            return;
        }

        try
        {
            bool boardChanged = false;
            Dictionary<string, CardData> newBoard = new Dictionary<string, CardData>();

            foreach (var child in e.Snapshot.Children)
            {
                var cardData = JsonConvert.DeserializeObject<CardData>(child.GetRawJsonValue());
                string cardId = child.Key;
                newBoard[cardId] = cardData;

                if (!board.ContainsKey(cardId) || board[cardId].IsFlipped != cardData.IsFlipped)
                {
                    boardChanged = true;
                    var card = tableCardList.Find(c => c.cardData.cardIndex.ToString() == cardId);
                    if (card != null && !card.isTurning)
                    {
                        if (cardData.IsFlipped != card.cardOpen)
                        {
                            card.cardOpen = cardData.IsFlipped;
                            StartCoroutine(TurnRoutine(card));
                        }
                    }
                }
            }

            board = newBoard;

            if (boardChanged && !isGameEnded && !isAnimating)
            {
                StartCoroutine(CheckGameEndRoutine());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"보드 상태 업데이트 중 오류 발생: {ex.Message}");
        }
    }

    private IEnumerator CheckGameEndRoutine()
    {
        // 모든 카드 애니메이션이 완료될 때까지 대기
        yield return new WaitUntil(() => !isAnimating);
        yield return new WaitForSeconds(0.5f);

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
        try
        {
            var snapshot = await roomRef.GetValueAsync();
            if (snapshot.Exists)
            {
                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
                // 현재 플레이어의 ID와 호스트 플레이어의 ID를 비교
                return roomData.HostPlayerId == GameRoomManager.Instance.currentPlayerId;
            }
            Debug.LogWarning("방 데이터가 존재하지 않습니다.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"호스트 확인 중 오류 발생: {ex.Message}");
            return false;
        }
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
        try
        {
            if (!e.Snapshot.Exists || memoryGamePanelUi == null || memoryGamePanelUi.cardPanel == null)
            {
                return;
            }

            foreach (var child in e.Snapshot.Children)
            {
                if (child == null || !child.Exists) continue;

                string playerId = child.Key;
                if (string.IsNullOrEmpty(playerId)) continue;

                try
                {
                    var playerData = JsonConvert.DeserializeObject<PlayerData>(child.GetRawJsonValue());
                    if (playerData == null) continue;

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
                catch (JsonException jsonEx)
                {
                    Debug.LogError($"플레이어 데이터 파싱 실패: {jsonEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"플레이어 데이터 업데이트 중 오류 발생: {ex.Message}");
        }
    }
}
