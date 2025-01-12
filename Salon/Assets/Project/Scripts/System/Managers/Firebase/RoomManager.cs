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
        private Dictionary<string, GameObject> instantiatedPlayers = new Dictionary<string, GameObject>();
        public LocalPlayer localPlayerPrefab;
        public RemotePlayer remotePlayerPrefab;
        public Transform spawnParent;
        private string CurrentChannel;
        private bool isQuitting = false;

        public void Initialize()
        {
            try
            {
                Debug.Log("[RoomManager] Initialize 시작");
                if (dbReference == null)
                {
                    Debug.Log("[RoomManager] 데이터베이스 참조 설정 시작");
                    dbReference = FirebaseManager.Instance.DbReference;
                    Debug.Log("[RoomManager] 데이터베이스 참조 설정 완료");
                }
                Debug.Log("[RoomManager] 플레이어 프리팹 로드 시작");
                localPlayerPrefab = ResourceManager.Instance.LoadResource<LocalPlayer>("Assets/Resources/Prefabs/Player/LocalPlayer.prefab");
                remotePlayerPrefab = ResourceManager.Instance.LoadResource<RemotePlayer>("Assets/Resources/Prefabs/Player/RemotePlayer.prefab");
                Debug.Log("[RoomManager] 플레이어 프리팹 로드 완료");

            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 초기화 실패: {ex.Message}");
            }
        }

        public void UnsubscribeFromChannel()
        {
            if (dbReference != null && !string.IsNullOrEmpty(CurrentChannel))
            {
                Debug.Log($"[RoomManager] {CurrentChannel} 채널의 이벤트 구독 해제 시작");
                var playersReference = dbReference.Child("Channels").Child(CurrentChannel).Child("Players");
                playersReference.ChildAdded -= OnPlayerAdded;
                playersReference.ChildRemoved -= OnPlayerRemoved;

                foreach (var player in instantiatedPlayers)
                {
                    var positionRef = playersReference.Child(player.Key).Child("Position");
                    positionRef.ValueChanged -= OnPositionChanged;
                }
                Debug.Log("[RoomManager] 이벤트 구독 해제 완료");
            }
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
            CurrentChannel = null;
            Debug.Log("[RoomManager] 모든 플레이어 오브젝트 제거 완료");
        }

        public async Task SubscribeToPlayerChanges(string channelName)
        {
            CurrentChannel = channelName;
            var playersReference = dbReference.Child("Channels").Child(channelName).Child("Players");

            var snapshot = await playersReference
                .OrderByChild("DisplayName")
                .StartAt(FirebaseManager.Instance.CurrentUserName + "\uf8ff")
                .GetValueAsync();

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
                        }
                    }
                }
            }

            playersReference.Child(FirebaseManager.Instance.CurrentUserName).ChildChanged -= OnPlayerUpdated;

            foreach (var player in instantiatedPlayers)
            {
                var positionRef = playersReference.Child(player.Key).Child("Position");
                positionRef.ValueChanged += OnPositionChanged;
            }

            playersReference.ChildAdded += OnPlayerAdded;
            playersReference.ChildRemoved += OnPlayerRemoved;
        }

        private void OnPositionChanged(object sender, ValueChangedEventArgs args)
        {
            if (!args.Snapshot.Exists) return;

            var displayName = args.Snapshot.Reference.Parent.Key;
            if (displayName == FirebaseManager.Instance.CurrentUserName) return;

            if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
            {
                var posData = JsonConvert.DeserializeObject<NetworkPositionData>(args.Snapshot.GetRawJsonValue());
                var player = playerObject.GetComponent<RemotePlayer>();
                if (player != null)
                {
                    player.GetNetworkPosition(posData);
                }
            }
        }

        private void OnPlayerAdded(object sender, ChildChangedEventArgs e)
        {
            if (e.Snapshot.Exists)
            {
                var displayName = e.Snapshot.Key;
                if (displayName != FirebaseManager.Instance.CurrentUserName)
                {
                    var playerData = JsonConvert.DeserializeObject<GamePlayerData>(e.Snapshot.GetRawJsonValue());
                    if (!instantiatedPlayers.ContainsKey(displayName))
                    {
                        InstantiatePlayer(displayName, playerData);
                        var positionRef = e.Snapshot.Reference.Child("Position");
                        positionRef.ValueChanged += OnPositionChanged;
                    }
                }
            }
        }

        private void OnPlayerRemoved(object sender, ChildChangedEventArgs e)
        {
            if (e.Snapshot.Exists)
            {
                var displayName = e.Snapshot.Key;
                if (instantiatedPlayers.ContainsKey(displayName))
                {
                    var positionRef = e.Snapshot.Reference.Child("Position");
                    positionRef.ValueChanged -= OnPositionChanged;

                    Destroy(instantiatedPlayers[displayName]);
                    instantiatedPlayers.Remove(displayName);
                }
            }
        }

        private void OnPlayerUpdated(object sender, ChildChangedEventArgs e)
        {
            if (e.Snapshot.Exists)
            {
                var displayName = e.Snapshot.Key;
                var playerData = JsonConvert.DeserializeObject<GamePlayerData>(e.Snapshot.GetRawJsonValue());

                if (instantiatedPlayers.ContainsKey(displayName))
                {
                    var player = instantiatedPlayers[displayName].GetComponent<RemotePlayer>();
                    if (player != null)
                    {
                        player.GetNetworkPosition(playerData.Position);
                    }
                }
            }
        }

        public void InstantiatePlayer(string displayName, GamePlayerData playerData, bool isLocalPlayer = false)
        {
            try
            {
                Debug.Log($"[RoomManager] 플레이어 생성 시작 - DisplayName: {displayName}, IsLocal: {isLocalPlayer}");

                // 스폰 위치를 Vector3.zero로 강제 설정
                Vector3 spawnPosition = Vector3.zero;
                Debug.Log($"[RoomManager] 스폰 위치를 (0,0,0)으로 강제 설정");

                if (isLocalPlayer)
                {
                    Debug.Log($"[RoomManager] 로컬 플레이어 프리팹 생성 시작");
                    if (localPlayerPrefab == null)
                    {
                        throw new Exception("[RoomManager] localPlayerPrefab이 null입니다.");
                    }

                    LocalPlayer localPlayer = Instantiate(localPlayerPrefab.gameObject, spawnPosition, Quaternion.identity, spawnParent).GetComponent<LocalPlayer>();
                    localPlayer.Initialize(displayName);
                    instantiatedPlayers[displayName] = localPlayer.gameObject;
                    Debug.Log($"[RoomManager] 로컬 플레이어 생성 완료 - GameObject: {localPlayer.gameObject.name}");
                }
                else
                {
                    Debug.Log($"[RoomManager] 리모트 플레이어 프리팹 생성 시작");
                    if (remotePlayerPrefab == null)
                    {
                        throw new Exception("[RoomManager] remotePlayerPrefab이 null입니다.");
                    }

                    RemotePlayer remotePlayer = Instantiate(remotePlayerPrefab.gameObject, spawnPosition, Quaternion.identity, spawnParent).GetComponent<RemotePlayer>();
                    remotePlayer.Initialize(displayName);
                    instantiatedPlayers[displayName] = remotePlayer.gameObject;
                    Debug.Log($"[RoomManager] 리모트 플레이어 생성 완료 - GameObject: {remotePlayer.gameObject.name}");
                }

                Debug.Log($"[RoomManager] 현재 생성된 플레이어 수: {instantiatedPlayers.Count}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 생성 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        private void UpdatePlayerPosition(GameObject playerObject, NetworkPositionData position)
        {
            var player = playerObject.GetComponent<RemotePlayer>();
            if (player != null)
            {
                player.GetNetworkPosition(position);
            }
        }
    }
}