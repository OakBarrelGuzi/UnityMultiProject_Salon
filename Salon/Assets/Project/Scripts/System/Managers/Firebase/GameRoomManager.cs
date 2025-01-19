using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Salon.System;
using Salon.Firebase.Database;

namespace Salon.Firebase
{
    public class GameRoomManager : Singleton<GameRoomManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference currentRoomRef;
        private string currentRoomId;
        private string currentChannelId;

        public Action<string> OnPlayerAdded;
        public Action<string> OnPlayerRemoved;
        public Action<string, string> OnCardFlipped; // 플레이어 ID와 카드 ID
        public Action<string> OnTurnChanged; // 턴이 변경되었을 때 호출

        private const int TURN_TIME_LIMIT = 30; // 제한 시간 30초
        private long lastCheckTime = 0;
        private bool isGameStarted = false;

        private async void Start()
        {
            await Initialize();
        }
        private void Update()
        {
            if (!isGameStarted) return;

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTime - lastCheckTime >= 1) // 매 1초마다 체크
            {
                lastCheckTime = currentTime;
                CheckTurnTimeout();
            }
        }
        public async Task Initialize()
        {
            dbReference = await GetDbReference();
            Debug.Log("[GameRoomManager] 초기화 완료");
        }

        private async Task<DatabaseReference> GetDbReference()
        {
            int maxRetries = 5;
            int currentRetry = 0;
            int delayMs = 1000;

            while (currentRetry < maxRetries)
            {
                if (FirebaseManager.Instance.DbReference != null)
                {
                    return FirebaseManager.Instance.DbReference;
                }

                Debug.Log($"[GameRoomManager] Firebase 데이터베이스 참조 대기 중... (시도 {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("[GameRoomManager] Firebase 데이터베이스 참조를 가져올 수 없습니다.");
        }

        public async Task<string> CreateRoom(string channelId, string playerInfo)
        {
            try
            {
                string newRoomName = Guid.NewGuid().ToString();
                GameRoomData newRoom = new GameRoomData(newRoomName, playerInfo);

                var hostPlayerData = new GamePlayerData(playerInfo);
                newRoom.Players.Add(playerInfo, hostPlayerData);

                // 첫 번째 턴은 방 호스트
                newRoom.GameState.CurrentTurnPlayerId = playerInfo;

                string roomJson = JsonConvert.SerializeObject(newRoom);
                await dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(newRoomName)
                    .SetRawJsonValueAsync(roomJson);

                Debug.Log($"[GameRoomManager] 채널 {channelId}에 새로운 방 생성됨: {newRoomName}");
                return newRoomName;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 방 생성 실패: {ex.Message}");
                return null;
            }
        }

        public async Task JoinRoom(string channelId, string roomName, string playerInfo)
        {
            try
            {
                var roomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomName);
                var snapshot = await roomRef.GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogError($"[GameRoomManager] 채널 {channelId}의 방 {roomName}이 존재하지 않습니다.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
                if (roomData.Players.ContainsKey(playerInfo))
                {
                    Debug.Log($"[GameRoomManager] 플레이어 {playerInfo}는 이미 방에 존재합니다.");
                    return;
                }

                var playerData = new GamePlayerData(playerInfo);
                roomData.Players.Add(playerInfo, playerData);

                string updatedRoomJson = JsonConvert.SerializeObject(roomData);
                await roomRef.SetRawJsonValueAsync(updatedRoomJson);

                currentRoomRef = roomRef;
                currentRoomId = roomName;
                currentChannelId = channelId;

                Debug.Log($"[GameRoomManager] 플레이어 {playerInfo}가 채널 {channelId}의 방 {roomName}에 입장했습니다.");

                await SubscribeToRoom(channelId, roomName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 방 참여 실패: {ex.Message}");
            }
        }

        private async Task SubscribeToRoom(string channelId, string roomName)
        {
            currentRoomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomName);

            var snapshot = await currentRoomRef.GetValueAsync();
            if (snapshot.Exists)
            {
                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
                foreach (var player in roomData.Players)
                {
                    OnPlayerAdded?.Invoke(player.Key);
                }

                if (roomData.GameState.CurrentTurnPlayerId != null)
                {
                    OnTurnChanged?.Invoke(roomData.GameState.CurrentTurnPlayerId);
                }
            }

            currentRoomRef.Child("Players").ChildAdded += HandlePlayerAdded;
            currentRoomRef.Child("Players").ChildRemoved += HandlePlayerRemoved;
            currentRoomRef.Child("GameState").ChildChanged += HandleTurnChanged;
        }
        public async Task<List<string>> GetRoomList(string channelId)
        {
            var roomsSnapshot = await dbReference.Child("Channels").Child(channelId).Child("GameRooms").GetValueAsync();

            if (!roomsSnapshot.Exists)
            {
                Debug.Log($"[GameRoomManager] 채널 {channelId}에 방이 없습니다.");
                return new List<string>();
            }

            List<string> roomIds = new List<string>();
            foreach (var room in roomsSnapshot.Children)
            {
                roomIds.Add(room.Key);
            }
            return roomIds;
        }
        private void HandlePlayerAdded(object sender, ChildChangedEventArgs e)
        {
            if (!e.Snapshot.Exists) return;

            var playerId = e.Snapshot.Key;
            OnPlayerAdded?.Invoke(playerId);
        }

        private void HandlePlayerRemoved(object sender, ChildChangedEventArgs e)
        {
            if (!e.Snapshot.Exists) return;

            var playerId = e.Snapshot.Key;
            OnPlayerRemoved?.Invoke(playerId);
        }

        private void HandleTurnChanged(object sender, ChildChangedEventArgs e)
        {
            if (!e.Snapshot.Exists) return;

            if (e.Snapshot.Key == "CurrentTurnPlayerId")
            {
                string newTurnPlayerId = e.Snapshot.Value.ToString();
                OnTurnChanged?.Invoke(newTurnPlayerId);
            }
        }

        public async Task FlipCard(string cardId, string playerId)
        {
            try
            {
                var roomSnapshot = await currentRoomRef.GetValueAsync();
                if (!roomSnapshot.Exists)
                {
                    Debug.LogError("[GameRoomManager] 방 데이터가 존재하지 않습니다.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(roomSnapshot.GetRawJsonValue());

                if (roomData.GameState.CurrentTurnPlayerId != playerId)
                {
                    Debug.LogWarning("[GameRoomManager] 현재 플레이어의 턴이 아닙니다.");
                    return;
                }

                var cardRef = currentRoomRef.Child("Board").Child(cardId);
                var snapshot = await cardRef.GetValueAsync();

                if (snapshot.Exists)
                {
                    var cardData = JsonConvert.DeserializeObject<CardData>(snapshot.GetRawJsonValue());
                    cardData.IsFlipped = !cardData.IsFlipped;
                    cardData.Owner = playerId;

                    await cardRef.SetRawJsonValueAsync(JsonConvert.SerializeObject(cardData));
                    Debug.Log($"[GameRoomManager] 카드 {cardId}가 {playerId}에 의해 뒤집어졌습니다.");

                    roomData.GameState.LastActionTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await UpdateTurnToNextPlayer(roomData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 카드 뒤집기 실패: {ex.Message}");
            }
        }
        private async Task UpdateTurnToNextPlayer(GameRoomData roomData)
        {
            var playerIds = new List<string>(roomData.Players.Keys);
            int currentPlayerIndex = playerIds.IndexOf(roomData.GameState.CurrentTurnPlayerId);

            int nextPlayerIndex = (currentPlayerIndex + 1) % playerIds.Count;
            roomData.GameState.CurrentTurnPlayerId = playerIds[nextPlayerIndex];

            roomData.GameState.LastActionTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await currentRoomRef.Child("GameState")
                .SetRawJsonValueAsync(JsonConvert.SerializeObject(roomData.GameState));

            Debug.Log($"[GameRoomManager] 턴이 {playerIds[nextPlayerIndex]}에게 넘어갔습니다.");
        }
        private async void CheckTurnTimeout()
        {
            try
            {
                var roomSnapshot = await currentRoomRef.GetValueAsync();
                if (!roomSnapshot.Exists)
                {
                    Debug.LogError("[GameRoomManager] 방 데이터가 존재하지 않습니다.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(roomSnapshot.GetRawJsonValue());
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // 제한 시간 초과 확인
                if (roomData.GameState.LastActionTimestamp + TURN_TIME_LIMIT < currentTime)
                {
                    Debug.LogWarning("[GameRoomManager] 턴 제한 시간이 초과되었습니다.");
                    await UpdateTurnToNextPlayer(roomData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 턴 시간 초과 확인 중 오류 발생: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            if (currentRoomRef != null)
            {
                currentRoomRef.Child("Players").ChildAdded -= HandlePlayerAdded;
                currentRoomRef.Child("Players").ChildRemoved -= HandlePlayerRemoved;
                currentRoomRef.Child("GameState").ChildChanged -= HandleTurnChanged;
            }
        }
    }
}
