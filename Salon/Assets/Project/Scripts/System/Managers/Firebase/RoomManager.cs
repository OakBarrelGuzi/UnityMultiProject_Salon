using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Salon.Firebase.Database;
using Salon.Character;

namespace Salon.Firebase
{
    public class RoomManager : MonoBehaviour
    {
        private DatabaseReference dbReference;
        private DatabaseReference channelsRef;
        private DatabaseReference currentChannelRef;
        private DatabaseReference currentChannelPlayersRef;
        private Query currentPlayersQuery;
        private Dictionary<string, GameObject> instantiatedPlayers = new Dictionary<string, GameObject>();
        private Dictionary<string, Query> playerPositionQueries = new Dictionary<string, Query>();
        public LocalPlayer localPlayerPrefab;
        public RemotePlayer remotePlayerPrefab;
        public Transform spawnParent;
        private string currentChannel;

        public void Initialize()
        {
            try
            {
                Debug.Log("[RoomManager] Initialize 시작");
                if (dbReference == null)
                {
                    Debug.Log("[RoomManager] 데이터베이스 참조 설정 시작");
                    dbReference = FirebaseManager.Instance.DbReference;
                    channelsRef = dbReference.Child("Channels");
                    Debug.Log("[RoomManager] 데이터베이스 참조 설정 완료");
                }
                Debug.Log("[RoomManager] 플레이어 프리팹 로드 시작");
                localPlayerPrefab = Resources.Load<LocalPlayer>("Prefabs/Player/LocalPlayer");
                remotePlayerPrefab = Resources.Load<RemotePlayer>("Prefabs/Player/RemotePlayer");
                Debug.Log("[RoomManager] 플레이어 프리팹 로드 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 초기화 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }

        private void UpdateChannelReferences(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return;

            currentChannelRef = channelsRef.Child(channelName);
            currentChannelPlayersRef = currentChannelRef.Child("Players");
            currentChannel = channelName;
            Debug.Log($"[RoomManager] 채널 레퍼런스 업데이트 완료: {channelName}");
        }

        private void ClearChannelReferences()
        {
            currentChannelRef = null;
            currentChannelPlayersRef = null;
            currentPlayersQuery = null;
            currentChannel = null;
            Debug.Log("[RoomManager] 채널 레퍼런스 초기화 완료");
        }

        public void UnsubscribeFromChannel()
        {
            if (currentPlayersQuery != null)
            {
                currentPlayersQuery.ChildAdded -= OnPlayerAdded;
                currentPlayersQuery.ChildRemoved -= OnPlayerRemoved;
                currentPlayersQuery = null;
            }

            foreach (var query in playerPositionQueries.Values)
            {
                query.ValueChanged -= OnPositionChanged;
            }
            playerPositionQueries.Clear();

            ClearChannelReferences();
            Debug.Log("[RoomManager] 채널 구독 해제 완료");
        }

        public void DestroyAllPlayers()
        {
            Debug.Log("[RoomManager] 모든 플레이어 오브젝트 제거 시작");
            foreach (var player in instantiatedPlayers)
            {
                if (player.Value != null)
                {
                    Destroy(player.Value);
                }
            }
            instantiatedPlayers.Clear();
            Debug.Log("[RoomManager] 모든 플레이어 오브젝트 제거 완료");
        }

        public async Task SubscribeToPlayerChanges(string channelName)
        {
            try
            {
                Debug.Log($"[RoomManager] 플레이어 변경사항 구독 시작: {channelName}");
                UpdateChannelReferences(channelName);

                // 기존 플레이어 로드
                var snapshot = await currentChannelPlayersRef.GetValueAsync();
                if (snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        var displayName = child.Key;
                        if (displayName != FirebaseManager.Instance.CurrentUserName)
                        {
                            var playerData = JsonConvert.DeserializeObject<GamePlayerData>(child.GetRawJsonValue());
                            if (!instantiatedPlayers.ContainsKey(displayName))
                            {
                                InstantiatePlayer(displayName, playerData);
                                SubscribeToPlayerPosition(displayName);
                            }
                        }
                    }
                }

                // 플레이어 변경사항 구독
                currentPlayersQuery = currentChannelPlayersRef;
                currentPlayersQuery.ChildAdded += OnPlayerAdded;
                currentPlayersQuery.ChildRemoved += OnPlayerRemoved;

                Debug.Log("[RoomManager] 플레이어 변경사항 구독 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 변경사항 구독 실패: {ex.Message}");
                throw;
            }
        }

        private void SubscribeToPlayerPosition(string displayName)
        {
            try
            {
                if (playerPositionQueries.ContainsKey(displayName))
                {
                    playerPositionQueries[displayName].ValueChanged -= OnPositionChanged;
                }

                var positionQuery = currentChannelPlayersRef.Child(displayName).Child("Position");
                positionQuery.ValueChanged += OnPositionChanged;
                playerPositionQueries[displayName] = positionQuery;
                Debug.Log($"[RoomManager] {displayName}의 위치 변경 구독 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 위치 구독 실패: {ex.Message}");
            }
        }

        private void UnsubscribeFromPlayerPosition(string displayName)
        {
            if (playerPositionQueries.TryGetValue(displayName, out var query))
            {
                query.ValueChanged -= OnPositionChanged;
                playerPositionQueries.Remove(displayName);
                Debug.Log($"[RoomManager] {displayName}의 위치 변경 구독 해제 완료");
            }
        }

        private void OnPositionChanged(object sender, ValueChangedEventArgs args)
        {
            if (!args.Snapshot.Exists)
            {
                Debug.Log("[RoomManager] OnPositionChanged: 스냅샷이 존재하지 않음");
                return;
            }

            var displayName = args.Snapshot.Reference.Parent.Key;
            Debug.Log($"[RoomManager] OnPositionChanged: 플레이어 {displayName}의 위치 변경 감지");

            if (displayName == FirebaseManager.Instance.CurrentUserName)
            {
                Debug.Log("[RoomManager] OnPositionChanged: 로컬 플레이어의 위치 변경은 무시");
                return;
            }

            if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
            {
                var posData = JsonConvert.DeserializeObject<NetworkPositionData>(args.Snapshot.GetRawJsonValue());
                var player = playerObject.GetComponent<RemotePlayer>();
                if (player != null)
                {
                    player.GetNetworkPosition(posData);
                }
                else
                {
                    Debug.LogError($"[RoomManager] OnPositionChanged: {displayName}의 RemotePlayer 컴포넌트를 찾을 수 없음");
                }
            }
            else
            {
                Debug.LogWarning($"[RoomManager] OnPositionChanged: {displayName}의 GameObject를 찾을 수 없음");
            }
        }

        private void OnPlayerAdded(object sender, ChildChangedEventArgs e)
        {
            if (!e.Snapshot.Exists) return;

            var displayName = e.Snapshot.Key;
            if (displayName == FirebaseManager.Instance.CurrentUserName) return;

            try
            {
                Debug.Log($"[RoomManager] OnPlayerAdded: 새로운 플레이어 감지 - {displayName}");
                var playerData = JsonConvert.DeserializeObject<GamePlayerData>(e.Snapshot.GetRawJsonValue());

                if (!instantiatedPlayers.ContainsKey(displayName))
                {
                    InstantiatePlayer(displayName, playerData);
                    SubscribeToPlayerPosition(displayName);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 추가 처리 실패: {ex.Message}");
            }
        }

        private void OnPlayerRemoved(object sender, ChildChangedEventArgs e)
        {
            if (!e.Snapshot.Exists) return;

            var displayName = e.Snapshot.Key;
            try
            {
                Debug.Log($"[RoomManager] OnPlayerRemoved: 플레이어 제거 - {displayName}");
                UnsubscribeFromPlayerPosition(displayName);

                if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
                {
                    DestroyImmediate(playerObject);
                    instantiatedPlayers.Remove(displayName);
                    Debug.Log($"[RoomManager] {displayName}의 플레이어 오브젝트 제거 완료");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 제거 처리 실패: {ex.Message}");
            }
        }

        public void InstantiatePlayer(string displayName, GamePlayerData playerData, bool isLocalPlayer = false)
        {
            try
            {
                Debug.Log($"[RoomManager] 플레이어 생성 시작 - DisplayName: {displayName}, IsLocal: {isLocalPlayer}");
                Vector3 spawnPosition = Vector3.zero;

                if (isLocalPlayer)
                {
                    if (localPlayerPrefab == null)
                    {
                        throw new Exception("[RoomManager] localPlayerPrefab이 null입니다.");
                    }

                    LocalPlayer localPlayer = Instantiate(localPlayerPrefab.gameObject, spawnPosition, Quaternion.identity, spawnParent).GetComponent<LocalPlayer>();
                    localPlayer.Initialize(displayName);
                    instantiatedPlayers[displayName] = localPlayer.gameObject;
                }
                else
                {
                    if (remotePlayerPrefab == null)
                    {
                        throw new Exception("[RoomManager] remotePlayerPrefab이 null입니다.");
                    }

                    RemotePlayer remotePlayer = Instantiate(remotePlayerPrefab.gameObject, spawnPosition, Quaternion.identity, spawnParent).GetComponent<RemotePlayer>();
                    remotePlayer.Initialize(displayName);
                    instantiatedPlayers[displayName] = remotePlayer.gameObject;
                }

                Debug.Log($"[RoomManager] 플레이어 생성 완료 - {displayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 생성 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }
    }
}