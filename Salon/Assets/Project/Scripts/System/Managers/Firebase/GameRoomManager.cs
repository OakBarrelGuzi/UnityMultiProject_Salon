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
        private DatabaseReference roomsRef;
        private DatabaseReference currentRoomRef;
        private string currentRoom;

        public Action<string> OnPlayerAdded;
        public Action<string> OnPlayerRemoved;
        public Action<string, string> OnCardFlipped; // �÷��̾� ID�� ī�� ID
        public Action<string> OnTurnChanged; // ���� ����Ǿ��� �� ȣ��

        private const int TURN_TIME_LIMIT = 30; // ���� �ð� 30��
        private long lastCheckTime = 0;

        private async void Start()
        {
            await Initialize();
        }
        private void Update()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTime - lastCheckTime >= 1) // �� 1�ʸ��� üũ
            {
                lastCheckTime = currentTime;
                CheckTurnTimeout();
            }
        }
        public async Task Initialize()
        {
            dbReference = await GetDbReference();
            roomsRef = dbReference.Child("GameRooms");
            Debug.Log("[GameRoomManager] �ʱ�ȭ �Ϸ�");
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

                Debug.Log($"[GameRoomManager] Firebase �����ͺ��̽� ���� ��� ��... (�õ� {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("[GameRoomManager] Firebase �����ͺ��̽� ������ ������ �� �����ϴ�.");
        }

        public async Task<string> CreateRoom(string hostPlayer, string displayName)
        {
            try
            {
                string newRoomName = Guid.NewGuid().ToString(); // ���� Room ID ����
                GameRoomData newRoom = new GameRoomData(newRoomName, hostPlayer);

                var hostPlayerData = new GamePlayerData(displayName);
                newRoom.Players.Add(hostPlayer, hostPlayerData);

                // ù ��° ���� �� ȣ��Ʈ�� ����
                newRoom.GameState.CurrentTurnPlayerId = hostPlayer;

                string roomJson = JsonConvert.SerializeObject(newRoom);
                await roomsRef.Child(newRoomName).SetRawJsonValueAsync(roomJson);

                Debug.Log($"[GameRoomManager] ���ο� �� ������: {newRoomName}");
                return newRoomName;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] �� ���� ����: {ex.Message}");
                return null;
            }
        }

        public async Task JoinRoom(string roomName, string playerId, string displayName)
        {
            try
            {
                var snapshot = await roomsRef.Child(roomName).GetValueAsync();
                if (!snapshot.Exists)
                {
                    Debug.LogError($"[GameRoomManager] �� {roomName}�� �������� �ʽ��ϴ�.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
                if (roomData.Players.ContainsKey(playerId))
                {
                    Debug.Log($"[GameRoomManager] �÷��̾� {playerId}�� �̹� �濡 �����մϴ�.");
                    return;
                }

                var playerData = new GamePlayerData(displayName);
                roomData.Players.Add(playerId, playerData);

                string updatedRoomJson = JsonConvert.SerializeObject(roomData);
                await roomsRef.Child(roomName).SetRawJsonValueAsync(updatedRoomJson);

                currentRoom = roomName;
                Debug.Log($"[GameRoomManager] �÷��̾� {playerId}�� �� {roomName}�� �����߽��ϴ�.");

                await SubscribeToRoom(roomName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] �� ���� ����: {ex.Message}");
            }
        }

        private async Task SubscribeToRoom(string roomId)
        {
            currentRoomRef = roomsRef.Child(roomId);

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
            if (string.IsNullOrEmpty(currentRoom)) return;

            try
            {
                var roomSnapshot = await currentRoomRef.GetValueAsync();
                if (!roomSnapshot.Exists)
                {
                    Debug.LogError("[GameRoomManager] �� �����Ͱ� �������� �ʽ��ϴ�.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(roomSnapshot.GetRawJsonValue());

                if (roomData.GameState.CurrentTurnPlayerId != playerId)
                {
                    Debug.LogWarning("[GameRoomManager] ���� �÷��̾��� ���� �ƴմϴ�.");
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
                    Debug.Log($"[GameRoomManager] ī�� {cardId}�� {playerId}�� ���� �����������ϴ�.");

                    roomData.GameState.LastActionTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await UpdateTurnToNextPlayer(roomData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] ī�� ������ ����: {ex.Message}");
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

            Debug.Log($"[GameRoomManager] ���� {playerIds[nextPlayerIndex]}���� �Ѿ���ϴ�.");
        }
        private async void CheckTurnTimeout()
        {
            if (string.IsNullOrEmpty(currentRoom)) return;

            try
            {
                var roomSnapshot = await currentRoomRef.GetValueAsync();
                if (!roomSnapshot.Exists)
                {
                    Debug.LogError("[GameRoomManager] �� �����Ͱ� �������� �ʽ��ϴ�.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(roomSnapshot.GetRawJsonValue());
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // ���� �ð� �ʰ� Ȯ��
                if (roomData.GameState.LastActionTimestamp + TURN_TIME_LIMIT < currentTime)
                {
                    Debug.LogWarning("[GameRoomManager] �� ���� �ð��� �ʰ��Ǿ����ϴ�.");
                    await UpdateTurnToNextPlayer(roomData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] �� �ð� �ʰ� Ȯ�� �� ���� �߻�: {ex.Message}");
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
