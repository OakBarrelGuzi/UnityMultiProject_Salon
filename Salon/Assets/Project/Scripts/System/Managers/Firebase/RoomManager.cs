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
        public GameObject playerPrefab;
        public Transform spawnParent;

        private string currentRoom = "Channel1";

        private async void OnEnable()
        {
            try
            {
                if (dbReference == null)
                    dbReference = FirebaseManager.Instance.DbReference;

                await SubscribeToPlayerChanges(currentRoom);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Firebase 초기화 실패: {ex.Message}");
            }
        }

        private async Task SubscribeToPlayerChanges(string channelName)
        {
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
            Vector3 spawnPosition = playerData.Position.GetPosition() ?? Vector3.zero;
            var playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity, spawnParent);
            playerObject.name = displayName;

            if (isLocalPlayer)
            {
                var localPlayer = playerObject.AddComponent<LocalPlayer>();
                var remotePlayer = playerObject.GetComponent<RemotePlayer>();
                if (remotePlayer != null) Destroy(remotePlayer);

                localPlayer.Initialize(displayName);
                instantiatedPlayers[displayName] = playerObject;
            }
            else
            {
                var remotePlayer = playerObject.AddComponent<RemotePlayer>();
                var localPlayer = playerObject.GetComponent<LocalPlayer>();
                if (localPlayer != null) Destroy(localPlayer);

                remotePlayer.Initialize(displayName);
                instantiatedPlayers[displayName] = playerObject;
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

        private void OnDisable()
        {
            if (dbReference != null)
            {
                var playersReference = dbReference.Child("Channels").Child(currentRoom).Child("Players");
                playersReference.ChildAdded -= OnPlayerAdded;
                playersReference.ChildRemoved -= OnPlayerRemoved;

                foreach (var player in instantiatedPlayers)
                {
                    var positionRef = playersReference.Child(player.Key).Child("Position");
                    positionRef.ValueChanged -= OnPositionChanged;
                }
            }
        }
    }
}