using UnityEngine;
using Firebase.Database;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Salon.System;
using Salon.Firebase.Database;
using Salon.DartGame;

namespace Salon.Firebase
{
    public class GameRoomManager : Singleton<GameRoomManager>
    {
        private DatabaseReference dbReference;
        public RoomCreationUI roomCreationUI;
        public string currentRoomId;
        public string currentChannelId;
        public string currentPlayerId;

        private async void Start()
        {
            await Initialize();
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
        /// <summary>
        /// 랜덤 방에 참가하거나 새 방 생성
        /// </summary>
        public async Task JoinOrCreateRandomRoom(string channelId, string playerInfo)
        {
            currentChannelId = channelId;
            currentPlayerId = playerInfo;

            // 1. 방 목록 가져오기
            var roomList = await GetRoomList(channelId);
            UIManager.Instance.OpenPanel(PanelType.PartyRoom);
            await Task.Delay(3000);
            roomCreationUI = UIManager.Instance.GetComponentInChildren<RoomCreationUI>();
            // 2. 빈 방 찾기
            foreach (string roomId in roomList)
            {
                var roomData = await GetRoomData(channelId, roomId);
                if (roomData != null && roomData.Players.Count < 2 && roomData.IsActive)
                {
                    await JoinRoom(channelId, roomId, playerInfo);
                    return;
                }
            }

            // 3. 빈 방이 없으면 새 방 생성
            string newRoomId = await CreateRoom(channelId, playerInfo);
            if (!string.IsNullOrEmpty(newRoomId))
            {
                await SetHostAsFirstTurn(channelId, newRoomId, playerInfo);
                Debug.Log($"[GameRoomManager] 새로운 방 생성 및 참가: {newRoomId}");
            }
            else
                Debug.LogError("[GameRoomManager] 방 생성 실패");
        }

        /// <summary>
        /// 방 목록 가져오기
        /// </summary>
        public async Task<List<string>> GetRoomList(string channelId)
        {
            var roomsSnapshot = await dbReference.Child("Channels").Child(channelId).Child("GameRooms").GetValueAsync();
            List<string> roomIds = new List<string>();

            if (roomsSnapshot.Exists)
            {
                foreach (var room in roomsSnapshot.Children)
                {
                    roomIds.Add(room.Key);
                }
            }
            return roomIds;
        }

        /// <summary>
        /// 특정 방의 데이터 가져오기
        /// </summary>
        private async Task<GameRoomData> GetRoomData(string channelId, string roomId)
        {
            var roomSnapshot = await dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId).GetValueAsync();
            if (roomSnapshot.Exists)
            {
                return JsonConvert.DeserializeObject<GameRoomData>(roomSnapshot.GetRawJsonValue());
            }
            return null;
        }

        /// <summary>
        /// 방 생성
        /// </summary>
        public async Task<string> CreateRoom(string channelId, string hostPlayerId)
        {
            try
            {
                string newRoomId = Guid.NewGuid().ToString();
                GameRoomData newRoom = new GameRoomData(newRoomId, hostPlayerId);

                // Firebase에 방 데이터 저장
                string roomJson = JsonConvert.SerializeObject(newRoom);
                await dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(newRoomId).SetRawJsonValueAsync(roomJson);

                currentRoomId = newRoomId;

                roomCreationUI.SetRoomData(newRoomId, channelId, hostPlayerId);
                Debug.Log($"[GameRoomManager] 방 생성 완료: {newRoomId}");
                return newRoomId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 방 생성 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 방에 참가
        /// </summary>
        public async Task JoinRoom(string channelId, string roomId, string playerInfo)
        {
            roomCreationUI.OnFind();
            await Task.Delay(3000);
            try
            {
                var roomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId);
                var snapshot = await roomRef.GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogError($"[GameRoomManager] 방 {roomId}이 존재하지 않습니다.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
                if (roomData.Players.ContainsKey(playerInfo))
                {
                    Debug.Log($"[GameRoomManager] 플레이어 {playerInfo}는 이미 방에 존재합니다.");
                    return;
                }

                roomData.Players.Add(playerInfo, new GamePlayerData(playerInfo));
                if (roomData.Players.Count == 1) // 처음 플레이어가 첫턴
                    roomData.GameState.CurrentTurnPlayerId = playerInfo;

                string updatedRoomJson = JsonConvert.SerializeObject(roomData);
                await roomRef.SetRawJsonValueAsync(updatedRoomJson);

                currentRoomId = roomId;

                roomCreationUI.SetRoomData(roomId, channelId, playerInfo);
                Debug.Log($"[GameRoomManager] 방 참가 완료: {roomId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 방 참가 실패: {ex.Message}");
            }
        }
        private async Task SetHostAsFirstTurn(string channelId, string roomId, string hostPlayerId)
        {
            try
            {
                var roomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId);
                var snapshot = await roomRef.GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogError($"[GameRoomManager] 방 {roomId}이 존재하지 않습니다.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());
                roomData.GameState.CurrentTurnPlayerId = hostPlayerId;

                string updatedRoomJson = JsonConvert.SerializeObject(roomData);
                await roomRef.SetRawJsonValueAsync(updatedRoomJson);

                Debug.Log($"[GameRoomManager] 방 {roomId}의 첫 턴이 {hostPlayerId}로 설정되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 첫 턴 설정 실패: {ex.Message}");
            }
        }

        public async Task DeleteRoom(string channelId, string roomId, string playerId)
        {
            try
            {
                var roomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId);
                var snapshot = await roomRef.GetValueAsync();

                if (!snapshot.Exists)
                {
                    Debug.LogError($"[GameRoomManager] 방 {roomId}이 존재하지 않습니다.");
                    return;
                }

                var roomData = JsonConvert.DeserializeObject<GameRoomData>(snapshot.GetRawJsonValue());

                // 방 호스트만 삭제 가능
                if (roomData.HostPlayerId != playerId)
                {
                    Debug.LogError($"[GameRoomManager] 플레이어 {playerId}는 방 {roomId}을 삭제할 권한이 없습니다.");
                    return;
                }

                // Firebase에서 방 데이터 삭제
                await roomRef.RemoveValueAsync();
                Debug.Log($"[GameRoomManager] 방 {roomId}이 삭제되었습니다.");

                // 현재 방 ID 초기화
                if (currentRoomId == roomId)
                {
                    currentRoomId = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 방 삭제 실패: {ex.Message}");
            }
        }
    }
}
