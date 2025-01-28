using UnityEngine;
using Firebase.Database;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Salon.Firebase.Database;
using System.Threading.Tasks;
using Salon.Character;
using Salon.System;
using Character;

namespace Salon.Firebase
{
    public class RoomManager : Singleton<RoomManager>
    {
        private DatabaseReference dbReference;
        private DatabaseReference channelsRef;
        public DatabaseReference currentChannelRef { get; private set; }
        public DatabaseReference CurrentChannelPlayersRef { get; private set; }
        private Dictionary<string, GameObject> instantiatedPlayers = new Dictionary<string, GameObject>();
        private Dictionary<string, Query> playerPositionQueries = new Dictionary<string, Query>();
        private Dictionary<string, Query> playerAnimationQueries = new Dictionary<string, Query>();
        private Dictionary<string, long> lastEmojiTimestamps = new Dictionary<string, long>();
        private Dictionary<string, long> lastAnimationTimestamps = new Dictionary<string, long>();

        public string CurrentChannelName => currentChannelRef?.Key;

        public LocalPlayer localPlayerPrefab;
        public RemotePlayer remotePlayerPrefab;
        public Transform spawnParent;
        public Vector3? savedPlayerPosition;

        private bool isTransitioningScene = false;

        public async Task Initialize()
        {
            try
            {
                Debug.Log("[RoomManager] Initialize 시작");
                if (dbReference == null)
                {
                    Debug.Log("[RoomManager] 데이터베이스 참조 설정 시작");
                    dbReference = await GetDbReference();
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

                Debug.Log($"[ChannelManager] Firebase 데이터베이스 참조 대기 중... (시도 {currentRetry + 1}/{maxRetries})");
                await Task.Delay(delayMs);
                currentRetry++;
                delayMs *= 2;
            }

            throw new Exception("[ChannelManager] Firebase 데이터베이스 참조를 가져올 수 없습니다.");
        }

        private void UpdateChannelReferences(string channelName)
        {
            if (string.IsNullOrEmpty(channelName)) return;

            currentChannelRef = channelsRef.Child(channelName);
            CurrentChannelPlayersRef = currentChannelRef.Child("Players");
            Debug.Log($"[RoomManager] 채널 레퍼런스 업데이트 완료: {channelName}");
        }

        private void ClearChannelReferences()
        {
            currentChannelRef = null;
            CurrentChannelPlayersRef = null;
            Debug.Log("[RoomManager] 채널 레퍼런스 초기화 완료");
        }

        public async Task UnsubscribeFromChannel()
        {
            try
            {
                isTransitioningScene = true;
                Debug.Log("[RoomManager] 채널 구독 해제 시작");

                // 먼저 모든 이벤트 구독을 해제
                if (CurrentChannelPlayersRef != null)
                {
                    CurrentChannelPlayersRef.ChildAdded -= OnPlayerAdded;
                    CurrentChannelPlayersRef.ChildRemoved -= OnPlayerRemoved;
                }

                foreach (var query in playerPositionQueries.Values)
                {
                    query.ValueChanged -= OnPositionChanged;
                }
                playerPositionQueries.Clear();

                foreach (var query in playerAnimationQueries.Values)
                {
                    query.ValueChanged -= OnAnimationChanged;
                }
                playerAnimationQueries.Clear();

                // 플레이어 오브젝트 제거
                DestroyAllPlayers();

                // 레퍼런스 초기화
                ClearChannelReferences();

                // 이벤트가 완전히 정리되도록 대기
                await Task.Delay(200);

                isTransitioningScene = false;
                Debug.Log("[RoomManager] 채널 구독 해제 완료");
            }
            catch (Exception ex)
            {
                isTransitioningScene = false;
                Debug.LogError($"[RoomManager] 채널 구독 해제 실패: {ex.Message}");
                throw;
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
            Debug.Log("[RoomManager] 모든 플레이어 오브젝트 제거 완료");
        }

        public async Task SubscribeToPlayerChanges(string channelName)
        {
            try
            {
                Debug.Log($"[RoomManager] 플레이어 변경사항 구독 시작: {channelName}");
                UpdateChannelReferences(channelName);

                // 먼저 이벤트 핸들러를 등록
                CurrentChannelPlayersRef.ChildAdded += OnPlayerAdded;
                CurrentChannelPlayersRef.ChildRemoved += OnPlayerRemoved;

                // 기존 플레이어 로드
                var snapshot = await CurrentChannelPlayersRef.GetValueAsync();
                if (snapshot.Exists)
                {
                    foreach (var child in snapshot.Children)
                    {
                        var displayName = child.Key;
                        if (displayName != FirebaseManager.Instance.GetCurrentDisplayName() &&
                            !instantiatedPlayers.ContainsKey(displayName))
                        {
                            try
                            {
                                var rawData = child.GetRawJsonValue();
                                var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData);
                                var playerData = new GamePlayerData(displayName);

                                if (jsonData != null)
                                {
                                    // 기본 필드들 파싱
                                    if (jsonData.ContainsKey("IsReady")) playerData.IsReady = Convert.ToBoolean(jsonData["IsReady"]);
                                    if (jsonData.ContainsKey("IsHost")) playerData.IsHost = Convert.ToBoolean(jsonData["IsHost"]);
                                    if (jsonData.ContainsKey("State")) playerData.State = (GamePlayerState)Enum.Parse(typeof(GamePlayerState), jsonData["State"].ToString());
                                    if (jsonData.ContainsKey("GameSpecificData"))
                                    {
                                        playerData.GameSpecificData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                            JsonConvert.SerializeObject(jsonData["GameSpecificData"]));
                                    }

                                    // 새로 추가된 필드들 파싱
                                    if (jsonData.ContainsKey("Position")) playerData.Position = jsonData["Position"].ToString();
                                    if (jsonData.ContainsKey("Animation"))
                                    {
                                        var animationValue = jsonData["Animation"];
                                        if (animationValue != null && animationValue.ToString() != "0")
                                        {
                                            playerData.Animation = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                                JsonConvert.SerializeObject(animationValue));
                                        }
                                        else
                                        {
                                            playerData.Animation = new Dictionary<string, object>();
                                        }
                                    }
                                    if (jsonData.ContainsKey("Emoji")) playerData.Emoji = jsonData["Emoji"].ToString();
                                    if (jsonData.ContainsKey("CharacterCustomization"))
                                    {
                                        playerData.CharacterCustomization = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                                            JsonConvert.SerializeObject(jsonData["CharacterCustomization"]));
                                    }
                                }

                                await InstantiatePlayer(displayName, playerData);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"[RoomManager] 플레이어 데이터 파싱 실패: {ex.Message}");
                                // 파싱 실패시 기본 플레이어 데이터로 생성
                                await InstantiatePlayer(displayName, new GamePlayerData(displayName));
                            }
                        }
                    }
                }

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
                print("[RoomManager] 플레이어 위치/애니메이션/이모지/커스터마이제이션 변경 구독 시작: " + displayName);
                if (playerPositionQueries.ContainsKey(displayName))
                {
                    print($"[RoomManager] 플레이어 위치 변경 구독 중복: {displayName}");
                    playerPositionQueries[displayName].ValueChanged -= OnPositionChanged;
                }
                print("[RoomManager] 플레이어 위치/애니메이션/이모지/커스터마이제이션 변경 구독 중복 해제 완료: " + displayName);

                var positionQuery = CurrentChannelPlayersRef.Child(displayName).Child("Position");
                positionQuery.ValueChanged += OnPositionChanged;

                var animationQuery = CurrentChannelPlayersRef.Child(displayName).Child("Animation");
                animationQuery.ValueChanged += OnAnimationChanged;

                var emojiQuery = CurrentChannelPlayersRef.Child(displayName).Child("Emoji");
                emojiQuery.ValueChanged += OnEmojiChanged;

                var customizationQuery = CurrentChannelPlayersRef.Child(displayName).Child("CharacterCustomization");
                customizationQuery.ValueChanged += OnCustomizationChanged;

                print("[RoomManager] 플레이어 위치/애니메이션/이모지/커스터마이제이션 변경 구독 완료: " + displayName);
                playerPositionQueries[displayName] = positionQuery;
                playerAnimationQueries[displayName] = animationQuery;
                Debug.Log($"[RoomManager] {displayName}의 위치/애니메이션/이모지/커스터마이제이션 변경 구독 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 위치/애니메이션/이모지/커스터마이제이션 구독 실패: {ex.Message}");
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

        private void OnEmojiChanged(object sender, ValueChangedEventArgs args)
        {
            if (!args.Snapshot.Exists)
            {
                Debug.Log("[RoomManager] OnEmojiChanged : 스냅샷이 존재하지 않음");
                return;
            }
            var displayName = args.Snapshot.Reference.Parent.Key;

            if (displayName == FirebaseManager.Instance.GetCurrentDisplayName())
            {
                Debug.Log("나 본인 OnEmojiChanged가 바뀜");
                return;
            }

            if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
            {
                var emojiData = args.Snapshot.Value as Dictionary<string, object>;
                if (emojiData != null &&
                    emojiData.ContainsKey("name") &&
                    emojiData.ContainsKey("timestamp"))
                {
                    string emojiName = emojiData["name"].ToString();
                    long timestamp = Convert.ToInt64(emojiData["timestamp"]);

                    // 타임스탬프가 더 최신이거나 처음 실행되는 경우에만 실행
                    if (!lastEmojiTimestamps.ContainsKey(displayName) ||
                        timestamp > lastEmojiTimestamps[displayName])
                    {
                        lastEmojiTimestamps[displayName] = timestamp;
                        var remotePlayer = playerObject.GetComponent<RemotePlayer>();
                        if (remotePlayer != null && remotePlayer.animController != null)
                        {
                            remotePlayer.animController.SetEmoji(emojiName);
                        }
                        else
                        {
                            Debug.LogError($"[RoomManager] {displayName}의 RemotePlayer 또는 AnimController가 null입니다.");
                        }
                    }
                }
            }
        }

        private void OnAnimationChanged(object sender, ValueChangedEventArgs args)
        {
            if (!args.Snapshot.Exists)
            {
                Debug.Log("[RoomManager] OnAnimationChanged : 스냅샷이 존재하지 않음");
                return;
            }
            var displayName = args.Snapshot.Reference.Parent.Key;

            if (displayName == FirebaseManager.Instance.GetCurrentDisplayName())
            {
                Debug.Log("나 본인 OnAnimationChanged가 바뀜");
                return;
            }

            if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
            {
                var animData = args.Snapshot.Value as Dictionary<string, object>;
                if (animData != null &&
                    animData.ContainsKey("name") &&
                    animData.ContainsKey("timestamp"))
                {
                    string animName = animData["name"].ToString();
                    long timestamp = Convert.ToInt64(animData["timestamp"]);

                    // 타임스탬프가 더 최신이거나 처음 실행되는 경우에만 실행
                    if (!lastAnimationTimestamps.ContainsKey(displayName) ||
                        timestamp > lastAnimationTimestamps[displayName])
                    {
                        lastAnimationTimestamps[displayName] = timestamp;
                        var remotePlayer = playerObject.GetComponent<RemotePlayer>();
                        if (remotePlayer != null)
                        {
                            remotePlayer.PlayAnimation(animName);
                        }
                        else
                        {
                            Debug.LogError($"[RoomManager] {displayName}의 RemotePlayer가 null입니다.");
                        }
                    }
                }
            }
        }

        private void OnCustomizationChanged(object sender, ValueChangedEventArgs args)
        {
            if (!args.Snapshot.Exists)
            {
                Debug.Log("[RoomManager] OnCustomizationChanged : 스냅샷이 존재하지 않음");
                return;
            }
            var displayName = args.Snapshot.Reference.Parent.Key;

            if (displayName == FirebaseManager.Instance.GetCurrentDisplayName())
            {
                Debug.Log("나 본인 OnCustomizationChanged가 바뀜");
                return;
            }

            if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
            {
                var customizationData = JsonConvert.DeserializeObject<Dictionary<string, string>>(args.Snapshot.GetRawJsonValue());
                var remotePlayer = playerObject.GetComponent<RemotePlayer>();
                if (remotePlayer != null)
                {
                    remotePlayer.UpdateCustomization(customizationData);
                }
                else
                {
                    Debug.LogError($"[RoomManager] {displayName}의 RemotePlayer가 null입니다.");
                }
            }
        }

        private void OnPositionChanged(object sender, ValueChangedEventArgs args)
        {
            // 씬 전환 중이면 위치 업데이트 무시
            if (isTransitioningScene)
            {
                return;
            }

            if (!args.Snapshot.Exists)
            {
                Debug.Log("[RoomManager] OnPositionChanged: 스냅샷이 존재하지 않음");
                return;
            }

            var displayName = args.Snapshot.Reference.Parent.Key;

            // 플레이어가 존재하는지 먼저 확인
            if (!instantiatedPlayers.ContainsKey(displayName))
            {
                return;
            }

            GameObject playerObject = instantiatedPlayers[displayName];
            if (playerObject == null)
            {
                Debug.LogWarning($"[RoomManager] OnPositionChanged: {displayName}의 GameObject가 null임");
                instantiatedPlayers.Remove(displayName);
                UnsubscribeFromPlayerPosition(displayName);
                return;
            }

            try
            {
                string compressedData = args.Snapshot.Value as string;
                if (string.IsNullOrEmpty(compressedData))
                {
                    return;
                }

                var player = playerObject.GetComponent<RemotePlayer>();
                if (player != null)
                {
                    player.GetNetworkPosition(compressedData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] OnPositionChanged: 위치 데이터 처리 중 오류 발생 - {ex.Message}");
            }
        }

        private async void OnPlayerAdded(object sender, ChildChangedEventArgs e)
        {
            if (!e.Snapshot.Exists) return;

            var displayName = e.Snapshot.Key;
            if (displayName == FirebaseManager.Instance.GetCurrentDisplayName()) return;

            try
            {
                Debug.Log($"[RoomManager] OnPlayerAdded: 새로운 플레이어 감지 - {displayName}");

                if (instantiatedPlayers.ContainsKey(displayName))
                {
                    Debug.Log($"[RoomManager] 플레이어 {displayName}는 이미 존재합니다. 중복 생성을 건너뜁니다.");
                    return;
                }

                await Task.Delay(100);

                if (instantiatedPlayers.ContainsKey(displayName))
                {
                    Debug.Log($"[RoomManager] 플레이어 {displayName}는 대기 후 이미 존재합니다. 중복 생성을 건너뜁니다.");
                    return;
                }

                var rawData = e.Snapshot.GetRawJsonValue();
                var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(rawData);
                var playerData = new GamePlayerData(displayName);

                if (jsonData != null)
                {
                    // 기본 필드들 파싱
                    if (jsonData.ContainsKey("IsReady")) playerData.IsReady = Convert.ToBoolean(jsonData["IsReady"]);
                    if (jsonData.ContainsKey("IsHost")) playerData.IsHost = Convert.ToBoolean(jsonData["IsHost"]);
                    if (jsonData.ContainsKey("State")) playerData.State = (GamePlayerState)Enum.Parse(typeof(GamePlayerState), jsonData["State"].ToString());
                    if (jsonData.ContainsKey("GameSpecificData"))
                    {
                        playerData.GameSpecificData = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                            JsonConvert.SerializeObject(jsonData["GameSpecificData"]));
                    }

                    // 새로 추가된 필드들 파싱
                    if (jsonData.ContainsKey("Position")) playerData.Position = jsonData["Position"].ToString();
                    if (jsonData.ContainsKey("Animation"))
                    {
                        var animationValue = jsonData["Animation"];
                        if (animationValue != null && animationValue.ToString() != "0")
                        {
                            playerData.Animation = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                JsonConvert.SerializeObject(animationValue));
                        }
                        else
                        {
                            playerData.Animation = new Dictionary<string, object>();
                        }
                    }
                    if (jsonData.ContainsKey("Emoji")) playerData.Emoji = jsonData["Emoji"].ToString();
                    if (jsonData.ContainsKey("CharacterCustomization"))
                    {
                        playerData.CharacterCustomization = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                            JsonConvert.SerializeObject(jsonData["CharacterCustomization"]));
                    }
                }

                await InstantiatePlayer(displayName, playerData);
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
                Debug.Log($"[RoomManager] OnPlayerRemoved: 플레이어 제거 시작 - {displayName}");

                UnsubscribeFromPlayerPosition(displayName);

                if (playerAnimationQueries.TryGetValue(displayName, out var animQuery))
                {
                    animQuery.ValueChanged -= OnAnimationChanged;
                    playerAnimationQueries.Remove(displayName);
                }

                if (instantiatedPlayers.TryGetValue(displayName, out GameObject playerObject))
                {
                    Debug.Log($"[RoomManager] 플레이어 오브젝트 제거 시도 - {displayName}, GameObject: {playerObject.name}");

                    if (playerObject != null)
                    {
                        Destroy(playerObject);
                        Debug.Log($"[RoomManager] 플레이어 오브젝트 Destroy 호출 완료 - {displayName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomManager] 플레이어 오브젝트가 이미 null임 - {displayName}");
                    }

                    instantiatedPlayers.Remove(displayName);
                    Debug.Log($"[RoomManager] 플레이어 {displayName} 제거 완료");
                }
                else
                {
                    Debug.LogWarning($"[RoomManager] 제거할 플레이어를 찾을 수 없음 - {displayName}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 제거 처리 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
            }
        }

        public async Task InstantiatePlayer(string displayName, GamePlayerData playerData, bool isLocalPlayer = false)
        {
            // 마지막으로 한번 더 중복 체크
            if (!isLocalPlayer && instantiatedPlayers.ContainsKey(displayName))
            {
                Debug.Log($"[RoomManager] InstantiatePlayer 시작 전 체크: 플레이어 {displayName}는 이미 존재합니다.");
                return;
            }

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

                    if (savedPlayerPosition.HasValue)
                    {
                        spawnPosition = savedPlayerPosition.Value;
                    }

                    LocalPlayer localPlayer = Instantiate(localPlayerPrefab.gameObject, spawnPosition, Quaternion.identity).GetComponent<LocalPlayer>();
                    localPlayer.Initialize(displayName);
                    instantiatedPlayers[displayName] = localPlayer.gameObject;
                    GameManager.Instance.player = localPlayer;

                    try
                    {
                        var userData = await FirebaseManager.Instance.GetUserData();
                        if (userData != null && userData.CharacterCustomization != null &&
                            userData.CharacterCustomization.selectedOptions != null)
                        {
                            var customizationManager = localPlayer.GetComponent<CharacterCustomizationManager>();
                            if (customizationManager != null)
                            {
                                customizationManager.ApplyCustomizationData(userData.CharacterCustomization.selectedOptions);
                            }
                        }
                        else
                        {
                            Debug.Log("커스터마이제이션 데이터가 없습니다.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RoomManager] 로컬 플레이어의 커스터마이제이션 데이터 로드 실패: {ex.Message}");
                    }
                }
                else
                {
                    if (remotePlayerPrefab == null)
                    {
                        throw new Exception("[RoomManager] remotePlayerPrefab이 null입니다.");
                    }

                    var positionSnapshot = await CurrentChannelPlayersRef.Child(displayName).Child("Position").GetValueAsync();
                    if (positionSnapshot.Exists)
                    {
                        string compressedData = positionSnapshot.Value as string;
                        if (!string.IsNullOrEmpty(compressedData))
                        {
                            var (position, direction, _) = NetworkPositionCompressor.DecompressToVectors(compressedData);
                            spawnPosition = position;
                            Debug.Log($"[RoomManager] 리모트 플레이어 {displayName}의 초기 위치: {position}");
                        }
                    }

                    if (instantiatedPlayers.ContainsKey(displayName))
                    {
                        Debug.Log($"[RoomManager] 위치 가져온 후 체크: 플레이어 {displayName}는 이미 존재합니다.");
                        return;
                    }

                    RemotePlayer remotePlayer = Instantiate(remotePlayerPrefab.gameObject, spawnPosition, Quaternion.identity).GetComponent<RemotePlayer>();
                    remotePlayer.Initialize(displayName);
                    instantiatedPlayers[displayName] = remotePlayer.gameObject;

                    try
                    {
                        var userSnapshot = await dbReference.Child("Users").Child(await FirebaseManager.Instance.GetUIDByDisplayName(displayName)).Child("CharacterCustomization").GetValueAsync();
                        if (userSnapshot.Exists)
                        {
                            var customizationData = JsonConvert.DeserializeObject<CharacterCustomizationData>(userSnapshot.GetRawJsonValue());
                            if (customizationData != null && customizationData.selectedOptions != null)
                            {
                                remotePlayer.UpdateCustomization(customizationData.selectedOptions);
                                Debug.Log($"[RoomManager] {displayName}의 초기 커스터마이제이션 데이터 적용 완료");
                            }
                        }
                        else
                        {
                            remotePlayer.UpdateCustomization(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RoomManager] {displayName}의 커스터마이제이션 데이터 로드 실패: {ex.Message}");
                    }

                    SubscribeToPlayerPosition(displayName);
                }

                Debug.Log($"[RoomManager] 플레이어 생성 완료 - {displayName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 플레이어 생성 실패: {ex.Message}\n스택 트레이스: {ex.StackTrace}");
                throw;
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                _ = UnsubscribeFromChannel();
            }
        }

        public async Task JoinChannel(string channelName)
        {
            try
            {
                Debug.Log($"[RoomManager] 채널 {channelName} 입장 시작");

                // 이전 채널에서 완전히 나가기
                await UnsubscribeFromChannel();

                // 새로운 채널 설정
                UpdateChannelReferences(channelName);

                // 로컬 플레이어 생성
                var playerData = new GamePlayerData(FirebaseManager.Instance.GetCurrentDisplayName());
                await InstantiatePlayer(FirebaseManager.Instance.GetCurrentDisplayName(), playerData, isLocalPlayer: true);

                // 다른 플레이어들 구독
                await SubscribeToPlayerChanges(channelName);

                Debug.Log($"[RoomManager] 채널 {channelName} 입장 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 채널 입장 실패: {ex.Message}");
                throw;
            }
        }

        public void RemoveAllListeners()
        {
            try
            {
                Debug.Log("[RoomManager] 모든 리스너 제거 시작");

                // 플레이어 추가/제거 리스너 제거
                if (CurrentChannelPlayersRef != null)
                {
                    CurrentChannelPlayersRef.ChildAdded -= OnPlayerAdded;
                    CurrentChannelPlayersRef.ChildRemoved -= OnPlayerRemoved;
                }

                // 위치 변경 리스너 제거
                foreach (var query in playerPositionQueries.Values)
                {
                    query.ValueChanged -= OnPositionChanged;
                }
                playerPositionQueries.Clear();

                // 애니메이션 변경 리스너 제거
                foreach (var query in playerAnimationQueries.Values)
                {
                    query.ValueChanged -= OnAnimationChanged;
                }
                playerAnimationQueries.Clear();

                // 레퍼런스 초기화
                ClearChannelReferences();

                Debug.Log("[RoomManager] 모든 리스너 제거 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RoomManager] 리스너 제거 중 오류 발생: {ex.Message}");
            }
        }
    }
}