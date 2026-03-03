using AvatarChat.Network.Models;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System;
using AvatarChat.Main;
using AvatarChat.Network.Signals;
using Zenject;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace AvatarChat.Network.Handlers
{
    public class NetworkRoomHandler : SubNetworkHandler, INetworkRoomHandler
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private INetworkHandler _networkHandler;

        [SerializeField] private int _maxRooms = 100;

        public NetworkList<NetworkRoom> ActiveRooms { get; private set; }
        private Dictionary<ulong, NetworkGuid> _playerToRoomMap = new();
        private Dictionary<NetworkGuid, Scene> _serverRoomScenes = new();
        private Dictionary<NetworkGuid, Scene> _clientRoomScenes = new();
        private RoomPlayerList _localRoomPlayers = RoomPlayerList.Empty;

        private readonly object _roomLock = new();
        private readonly object _clientSceneLock = new();
        private readonly Dictionary<string, NetworkGuid> _baseNameToInstance = new();
        private readonly Dictionary<string, NetworkGuid> _fullNameToInstance = new();

        private readonly Dictionary<NetworkGuid, CancellationTokenSource> _loadingCancellations = new();

        private void Awake() => ActiveRooms = new();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnect;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnect;

            foreach (var cts in _loadingCancellations.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _loadingCancellations.Clear();

            Debug.Log($"[NetworkRoomHandler] Despawned. Final state - Rooms: {ActiveRooms.Count}, Players: {_playerToRoomMap.Count}");
        }

        private void OnServerClientDisconnect(ulong clientId)
        {
            if (_playerToRoomMap.TryGetValue(clientId, out NetworkGuid instanceId))
            {
                HandlePlayerExit(instanceId, clientId);
            }
        }

        private void LogDictionarySizes()
        {
            var sb = new System.Text.StringBuilder(128);
            sb.Append("[NetworkRoomHandler] State - ")
              .Append("Rooms: ").Append(ActiveRooms.Count).Append("/").Append(_maxRooms).Append(", ")
              .Append("PlayerMappings: ").Append(_playerToRoomMap.Count).Append(", ")
              .Append("ServerScenes: ").Append(_serverRoomScenes.Count).Append(", ")
              .Append("ClientScenes: ").Append(_clientRoomScenes.Count).Append(", ")
              .Append("LoadingCancellations: ").Append(_loadingCancellations.Count);
            
            Debug.Log(sb.ToString());
        }

        private bool HasSuffix(string roomName) => roomName.Contains("_");

        private bool IsPlayerInRoom(ulong clientId) => _playerToRoomMap.ContainsKey(clientId);

        private bool TryRejectIfAlreadyInRoom(ulong clientId)
        {
            if (IsPlayerInRoom(clientId))
            {
                Debug.LogError($"[NetworkRoomHandler] Player {clientId} is already in a room. Operation rejected.");
                return true;
            }
            return false;
        }

        public void RequestJoinOrCreateRoom(FixedString128Bytes roomName, int maxPlayers)
        {
            JoinOrCreateRoomServerRpc(roomName, maxPlayers);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinOrCreateRoomServerRpc(FixedString128Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            
            if (TryRejectIfAlreadyInRoom(senderId)) return;
            
            NetworkGuid targetInstance = GetOrCreateRoomInternal(roomName, maxPlayers);
            JoinRoomInternal(targetInstance, senderId).Forget();
        }

        private NetworkGuid GetOrCreateRoomInternal(FixedString128Bytes roomName, int maxPlayers)
        {
            string name = roomName.ToString();
            bool hasSuffix = HasSuffix(name);
            string baseName = name.Split('_')[0];

            lock (_roomLock)
            {
                if (ActiveRooms.Count >= _maxRooms)
                {
                    Debug.LogError($"[NetworkRoomHandler] Max rooms ({_maxRooms}) reached. Cannot create room: {name}");
                    return default;
                }

                if (!hasSuffix)
                {
                    if (_baseNameToInstance.TryGetValue(baseName, out NetworkGuid existingId))
                        return existingId;
                }
                else
                {
                    if (_fullNameToInstance.TryGetValue(name, out NetworkGuid existingId))
                        return existingId;
                }

                NetworkGuid newInstance = Guid.NewGuid();
                var newRoom = new NetworkRoom
                {
                    RoomName = roomName,
                    InstanceId = newInstance,
                    MaxPlayers = maxPlayers,
                    CurrentPlayers = 0,
                    SceneLoaded = false
                };

                ActiveRooms.Add(newRoom);

                if (!hasSuffix)
                    _baseNameToInstance[baseName] = newInstance;
                else
                    _fullNameToInstance[name] = newInstance;

                LogDictionarySizes();
                Debug.Log($"[NetworkRoomHandler] Created room: {name} -> {newInstance} (Active: {ActiveRooms.Count}/{_maxRooms})");

                return newInstance;
            }
        }

        private int GetRoomIndexByInstanceId(NetworkGuid instanceId)
        {
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId)) return i;
            }
            return -1;
        }

        private async UniTask JoinRoomInternal(NetworkGuid instanceId, ulong clientId)
        {
            int roomIndex = GetRoomIndexByInstanceId(instanceId);
            if (roomIndex == -1)
            {
                Debug.LogError($"[NetworkRoomHandler] Room not found: {instanceId}");
                return;
            }

            var room = ActiveRooms[roomIndex];
            if (room.CurrentPlayers >= room.MaxPlayers)
            {
                Debug.LogWarning($"[NetworkRoomHandler] Room is full: {room.RoomName} ({room.CurrentPlayers}/{room.MaxPlayers})");
                return;
            }

            room.CurrentPlayers++;
            ActiveRooms[roomIndex] = room;
            _playerToRoomMap[clientId] = instanceId;

            var spawnHandler = _networkHandler.GetSubHandler<NetworkSpawnHandler>();
            spawnHandler?.TrackPlayerRoom(clientId, instanceId);

            SyncRoomPlayers(instanceId);

            if (!_serverRoomScenes.ContainsKey(instanceId))
            {
                var cts = new CancellationTokenSource();
                _loadingCancellations[instanceId] = cts;

                try
                {
                    await LoadSceneOnServerAsync(room.RoomName.ToString(), instanceId, cts.Token);

                    roomIndex = GetRoomIndexByInstanceId(instanceId);
                    if (roomIndex != -1)
                    {
                        var updatedRoom = ActiveRooms[roomIndex];
                        updatedRoom.SceneLoaded = true;
                        ActiveRooms[roomIndex] = updatedRoom;
                    }
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"[NetworkRoomHandler] Scene loading cancelled for room: {instanceId}");
                    _playerToRoomMap.Remove(clientId);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NetworkRoomHandler] Failed to load scene for room {instanceId}: {e.Message}");
                    _playerToRoomMap.Remove(clientId);
                    return;
                }
                finally
                {
                    if (_loadingCancellations.TryGetValue(instanceId, out var existingCts))
                    {
                        existingCts.Dispose();
                        _loadingCancellations.Remove(instanceId);
                    }
                }
            }

            MovePlayerToRoomScene(clientId, instanceId);
            LoadSceneForClientClientRpc(room.RoomName, instanceId, RpcTarget.Single(clientId, RpcTargetUse.Temp));

            _signalBus.Fire(new PlayerJoinedRoomSignal(clientId, instanceId.ToString()));
        }

        private async UniTask LoadSceneOnServerAsync(string fullRoomName, NetworkGuid roomId, CancellationToken token = default)
        {
            string sceneToLoad = fullRoomName.Split('_')[0];
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics2D);

            var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, parameters);
            
            while (!asyncOp.isDone)
            {
                if (token.IsCancellationRequested)
                {
                    Debug.LogWarning($"[NetworkRoomHandler] Scene loading cancelled: {sceneToLoad}");
                    asyncOp.allowSceneActivation = false;
                    throw new OperationCanceledException(token);
                }
                
                await UniTask.Yield();
            }

            if (asyncOp.progress < 0.9f)
            {
                Debug.LogError($"[NetworkRoomHandler] Failed to load scene: {sceneToLoad} (progress: {asyncOp.progress})");
                throw new Exception($"Scene loading failed: {sceneToLoad}");
            }

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            
            if (!loadedScene.IsValid())
            {
                Debug.LogError($"[NetworkRoomHandler] Loaded scene is invalid: {sceneToLoad}");
                throw new Exception("Loaded scene is invalid");
            }
            
            _serverRoomScenes[roomId] = loadedScene;
            Debug.Log($"[NetworkRoomHandler] Scene loaded successfully: {sceneToLoad} -> {roomId}");
        }

        private void MovePlayerToRoomScene(ulong clientId, NetworkGuid roomId)
        {
            if (_serverRoomScenes.TryGetValue(roomId, out Scene roomScene))
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                {
                    var playerObj = client.PlayerObject;
                    if (playerObj != null)
                        SceneManager.MoveGameObjectToScene(playerObj.gameObject, roomScene);
                }
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void LoadSceneForClientClientRpc(FixedString128Bytes roomName, NetworkGuid roomId, RpcParams delivery)
        {
            ExecuteClientSceneLoad(roomName.ToString(), roomId).Forget();
        }

        private async UniTaskVoid ExecuteClientSceneLoad(string roomName, NetworkGuid roomId)
        {
            Scene existingScene;
            lock (_clientSceneLock)
            {
                if (_clientRoomScenes.TryGetValue(roomId, out existingScene))
                {
                    NotifySceneLoadedServerRpc(roomId);
                    return;
                }
            }

            string sceneToLoad = roomName.Split('_')[0];
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics2D);
            var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, parameters);
            
            while (!asyncOp.isDone)
            {
                await UniTask.Yield();
            }

            if (asyncOp.progress < 0.9f)
            {
                Debug.LogError($"[NetworkRoomHandler] Client failed to load scene: {sceneToLoad} (progress: {asyncOp.progress})");
                return;
            }

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            
            if (!loadedScene.IsValid())
            {
                Debug.LogError($"[NetworkRoomHandler] Client loaded invalid scene: {sceneToLoad}");
                return;
            }

            lock (_clientSceneLock)
            {
                _clientRoomScenes[roomId] = loadedScene;
            }

            Debug.Log($"[NetworkRoomHandler] Client scene loaded: {sceneToLoad} -> {roomId}");
            NotifySceneLoadedServerRpc(roomId);
        }

        [Rpc(SendTo.Server)]
        private void NotifySceneLoadedServerRpc(NetworkGuid roomId, RpcParams rpcParams = default) { }

        private void HandlePlayerExit(NetworkGuid instanceId, ulong clientId)
        {
            if (_playerToRoomMap.ContainsKey(clientId))
                _playerToRoomMap.Remove(clientId);

            int index = GetRoomIndexByInstanceId(instanceId);
            if (index == -1)
            {
                Debug.LogWarning($"[NetworkRoomHandler] Player {clientId} exit: room not found {instanceId}");
                return;
            }

            int remaining = 0;
            foreach (var kv in _playerToRoomMap)
            {
                if (kv.Value.Equals(instanceId)) remaining++;
            }

            var room = ActiveRooms[index];
            room.CurrentPlayers = remaining;

            Debug.Log($"[NetworkRoomHandler] Player {clientId} left room {instanceId}. Remaining: {remaining}");

            SyncRoomPlayers(instanceId);

            _signalBus.Fire(new PlayerLeftRoomSignal(clientId, instanceId.ToString()));

            if (room.CurrentPlayers <= 0)
            {
                Debug.Log($"[NetworkRoomHandler] Room {instanceId} is empty, removing...");
                RemoveRoom(instanceId);
            }
            else
            {
                ActiveRooms[index] = room;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void LeaveRoomServerRpc(NetworkGuid instanceId, RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            
            if (!_playerToRoomMap.TryGetValue(clientId, out NetworkGuid currentRoomId))
            {
                Debug.LogError($"[NetworkRoomHandler] Player {clientId} is not in any room. Cannot leave.");
                return;
            }
            
            if (!currentRoomId.Equals(instanceId))
            {
                Debug.LogError($"[NetworkRoomHandler] Player {clientId} is not in room {instanceId}. Cannot leave.");
                return;
            }
            
            HandlePlayerExit(instanceId, clientId);

            ConfirmActionClientRpc(instanceId, false, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ConfirmActionClientRpc(NetworkGuid instanceId, bool isJoin, RpcParams delivery)
        {
            if (isJoin)
                _signalBus.Fire(new PlayerJoinedRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId.ToString()));
            else
                LeaveRoomLocalCleanup(instanceId);
        }

        private void LeaveRoomLocalCleanup(NetworkGuid instanceId)
        {
            if (_clientRoomScenes.TryGetValue(instanceId, out Scene scene))
            {
                if (scene.isLoaded)
                    _ = UnloadRoomSceneDelayed();
            }

            _localRoomPlayers = RoomPlayerList.Empty;
        }

        private async UniTaskVoid UnloadRoomSceneDelayed()
        {
            await UniTask.Delay(100);
            foreach (var scene in _clientRoomScenes.Values)
            {
                if (scene.isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
            }
            _clientRoomScenes.Clear();
        }

        private void SyncRoomPlayers(NetworkGuid instanceId)
        {
            if (!IsServer) return;

            var playerIds = new List<ulong>();
            foreach (var kv in _playerToRoomMap)
            {
                if (kv.Value.Equals(instanceId))
                    playerIds.Add(kv.Key);
            }

            var roomPlayerList = new RoomPlayerList
            {
                RoomId = instanceId,
                PlayerIds = playerIds.ToArray()
            };

            SyncRoomPlayersClientRpc(roomPlayerList);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SyncRoomPlayersClientRpc(RoomPlayerList roomPlayerList)
        {
            _localRoomPlayers = roomPlayerList;
        }

        public RoomPlayerList GetRoomPlayers() => _localRoomPlayers;

        public void RequestCreateRoom(FixedString128Bytes roomName, int maxPlayers) => CreateRoomServerRpc(roomName, maxPlayers);
        public void RequestJoinRoom(NetworkGuid instanceId) => JoinRoomServerRpc(instanceId);
        public void RequestLeaveRoom(NetworkGuid instanceId) => LeaveRoomServerRpc(instanceId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void CreateRoomServerRpc(FixedString128Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            
            if (TryRejectIfAlreadyInRoom(senderId)) return;
            
            NetworkGuid targetInstance = GetOrCreateRoomInternal(roomName, maxPlayers);
            JoinRoomInternal(targetInstance, senderId).Forget();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinRoomServerRpc(NetworkGuid instanceId, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            
            if (TryRejectIfAlreadyInRoom(senderId)) return;
            
            JoinRoomInternal(instanceId, senderId).Forget();
        }

        public void RemoveRoom(NetworkGuid instanceId)
        {
            if (!IsServer) return;

            if (_serverRoomScenes.TryGetValue(instanceId, out Scene scene))
            {
                if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
                _serverRoomScenes.Remove(instanceId);
            }

            int index = GetRoomIndexByInstanceId(instanceId);
            if (index != -1)
            {
                string rn = ActiveRooms[index].RoomName.ToString();
                lock (_roomLock)
                {
                    if (HasSuffix(rn))
                        _fullNameToInstance.Remove(rn);
                    else
                        _baseNameToInstance.Remove(rn.Split('_')[0]);
                }
                ActiveRooms.RemoveAt(index);
            }
        }

        public NetworkRoom GetRoom(NetworkGuid value)
        {
            int index = GetRoomIndexByInstanceId(value);
            return index != -1 ? ActiveRooms[index] : NetworkRoom.Empty;
        }

        internal NetworkRoom GetRoomByPlayer(ulong ownerClientId)
        {
            if (_playerToRoomMap.TryGetValue(ownerClientId, out NetworkGuid instanceId))
                return GetRoom(instanceId);

            return NetworkRoom.Empty;
        }

        public Scene GetRoomScene(NetworkGuid instanceId)
        {
            if (IsServer && _serverRoomScenes.TryGetValue(instanceId, out Scene serverScene)) return serverScene;
            if (!IsServer && _clientRoomScenes.TryGetValue(instanceId, out Scene clientScene)) return clientScene;
            return default;
        }
    }
}