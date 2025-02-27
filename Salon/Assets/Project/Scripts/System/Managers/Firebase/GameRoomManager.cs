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
        public DatabaseReference roomRef;
        public string currentRoomId;
        public string currentChannelId;
        public string currentPlayerId;

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
            try
            {
                if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(playerInfo))
                {
                    Debug.LogError("[GameRoomManager] 채널 ID 또는 플레이어 정보가 없습니다.");
                    return;
                }

                currentChannelId = channelId;
                currentPlayerId = playerInfo;

                Debug.Log($"[GameRoomManager] 방 참가/생성 시도 - 채널: {channelId}, 플레이어: {playerInfo}");

                // 1. 방 목록 가져오기
                var roomList = await GetRoomList(channelId);
                UIManager.Instance.OpenPanel(PanelType.PartyRoom);
                roomCreationUI = UIManager.Instance.GetComponentInChildren<RoomCreationUI>();

                // 2. 빈 방 찾기 (활성화된 방 중에서)
                foreach (string roomId in roomList)
                {
                    try
                    {
                        var roomData = await GetRoomData(channelId, roomId);
                        if (roomData == null) continue;

                        // 방이 유효한지 확인
                        if (!roomData.IsActive)
                        {
                            Debug.Log($"[GameRoomManager] 방 {roomId}이 비활성 상태입니다.");
                            continue;
                        }

                        // 플레이어가 이미 방에 있는지 확인
                        if (roomData.Players.ContainsKey(playerInfo))
                        {
                            Debug.Log($"[GameRoomManager] 플레이어 {playerInfo}는 이미 방 {roomId}에 있습니다.");
                            continue;
                        }

                        // 방이 가득 찼는지 확인
                        if (roomData.Players.Count >= 2)
                        {
                            Debug.Log($"[GameRoomManager] 방 {roomId}이 가득 찼습니다.");
                            continue;
                        }

                        Debug.Log($"[GameRoomManager] 참가 가능한 방 발견: {roomId}");
                        await JoinRoom(channelId, roomId, playerInfo);
                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GameRoomManager] 방 {roomId} 확인 중 오류 발생: {ex.Message}");
                        continue;
                    }
                }

                // 3. 참가 가능한 방이 없으면 새 방 생성
                Debug.Log("[GameRoomManager] 참가 가능한 방이 없어 새로운 방을 생성합니다.");
                string newRoomId = await CreateRoom(channelId, playerInfo);
                if (!string.IsNullOrEmpty(newRoomId))
                {
                    await SetHostAsFirstTurn(channelId, newRoomId, playerInfo);
                    Debug.Log($"[GameRoomManager] 새로운 방 생성 및 참가 완료: {newRoomId}");
                }
                else
                {
                    Debug.LogError("[GameRoomManager] 방 생성 실패");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] JoinOrCreateRandomRoom 실행 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
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
                newRoom.Players.Add(hostPlayerId, new PlayerData(hostPlayerId, true));

                string roomJson = JsonConvert.SerializeObject(newRoom);
                await dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(newRoomId).SetRawJsonValueAsync(roomJson);

                currentRoomId = newRoomId;
                roomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(newRoomId);

                await SetRoomDeletionOnDisconnect(channelId, newRoomId);
                roomCreationUI.SetRoomData(newRoomId, channelId, hostPlayerId);
                Debug.Log($"[GameRoomManager] 방 생성 완료: {newRoomId}");

                WaitForPlayerJoin(channelId, newRoomId);
                return newRoomId;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 방 생성 실패: {ex.Message}");
                return null;
            }
        }
        private void WaitForPlayerJoin(string channelId, string roomId)
        {
            var roomPlayersRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId).Child("Players");

            roomPlayersRef.ChildAdded += async (sender, e) =>
            {
                if (e.Snapshot.Exists && e.Snapshot.Key != currentPlayerId) // 현재 플레이어 제외
                {
                    Debug.Log($"[GameRoomManager] 새로운 플레이어 참가 감지: {e.Snapshot.Key}");

                    roomCreationUI.OnFind();
                    await Task.Delay(3000);
                    UIManager.Instance.CloseAllPanels();
                    ScenesManager.Instance.ChanageScene("MemoryGame");
                }
            };
        }
        /// <summary>
        /// 방에 참가
        /// </summary>
        public async Task JoinRoom(string channelId, string roomId, string playerInfo)
        {
            try
            {
                var roomRefTemp = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId);
                var snapshot = await roomRefTemp.GetValueAsync();

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

                roomData.Players[playerInfo] = new PlayerData(playerInfo, false);

                string updatedRoomJson = JsonConvert.SerializeObject(roomData);
                await roomRefTemp.SetRawJsonValueAsync(updatedRoomJson);

                currentRoomId = roomId;
                roomRef = roomRefTemp;

                await SetRoomDeletionOnDisconnect(channelId, roomId);
                roomCreationUI.SetRoomData(roomId, channelId, playerInfo);

                roomCreationUI.OnFind();
                await Task.Delay(3000);
                UIManager.Instance.CloseAllPanels();
                ScenesManager.Instance.ChanageScene("MemoryGame");
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
        public async Task SetRoomDeletionOnDisconnect(string channelId, string roomId)
        {
            try
            {
                var roomRef = dbReference.Child("Channels").Child(channelId).Child("GameRooms").Child(roomId);

                await roomRef.OnDisconnect().RemoveValue();
                Debug.Log($"[GameRoomManager] 클라이언트 연결 해제 시 방 {roomId} 자동 삭제 설정 완료.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameRoomManager] 연결 해제 시 방 삭제 설정 실패: {ex.Message}");
            }
        }
    }
}
