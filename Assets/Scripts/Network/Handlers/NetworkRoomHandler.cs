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

namespace AvatarChat.Network.Handlers
{
    public class NetworkRoomHandler : SubNetworkHandler, INetworkRoomHandler
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private INetworkHandler _networkHandler;

        public NetworkList<NetworkRoom> ActiveRooms { get; private set; }
        private Dictionary<ulong, NetworkGuid> _playerToRoomMap = new();
        private Dictionary<NetworkGuid, Scene> _serverRoomScenes = new();
        private Dictionary<NetworkGuid, Scene> _clientRoomScenes = new();

        private readonly object _roomLock = new();
        private readonly Dictionary<string, NetworkGuid> _baseNameToInstance = new();
        private readonly Dictionary<string, NetworkGuid> _fullNameToInstance = new();

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
        }

        private void OnServerClientDisconnect(ulong clientId)
        {
            if (_playerToRoomMap.TryGetValue(clientId, out NetworkGuid instanceId))
            {
                HandlePlayerExit(instanceId, clientId);
            }
        }

        private bool HasSuffix(string roomName) => roomName.Contains("_");

        public void RequestJoinOrCreateRoom(FixedString128Bytes roomName, int maxPlayers)
        {
            JoinOrCreateRoomServerRpc(roomName, maxPlayers);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinOrCreateRoomServerRpc(FixedString128Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
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
            if (roomIndex == -1) return;

            var room = ActiveRooms[roomIndex];
            if (room.CurrentPlayers >= room.MaxPlayers) return;

            room.CurrentPlayers++;
            ActiveRooms[roomIndex] = room;
            _playerToRoomMap[clientId] = instanceId;

            var spawnHandler = _networkHandler.GetSubHandler<NetworkSpawnHandler>();
            spawnHandler?.TrackPlayerRoom(clientId, instanceId);

            if (!_serverRoomScenes.ContainsKey(instanceId))
            {
                await LoadSceneOnServerAsync(room.RoomName.ToString(), instanceId);

                roomIndex = GetRoomIndexByInstanceId(instanceId);
                if (roomIndex != -1)
                {
                    var updatedRoom = ActiveRooms[roomIndex];
                    updatedRoom.SceneLoaded = true;
                    ActiveRooms[roomIndex] = updatedRoom;
                }
            }

            MovePlayerToRoomScene(clientId, instanceId);

            LoadSceneForClientClientRpc(room.RoomName, instanceId, RpcTarget.Single(clientId, RpcTargetUse.Temp));

            _signalBus.Fire(new PlayerJoinedRoomSignal(clientId, instanceId.ToString()));
        }

        private async UniTask LoadSceneOnServerAsync(string fullRoomName, NetworkGuid roomId)
        {
            string sceneToLoad = fullRoomName.Split('_')[0];
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics2D);

            var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, parameters);
            await asyncOp;

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            _serverRoomScenes[roomId] = loadedScene;
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
            // Если у клиента эта сцена уже загружена (например, переподключение), выходим
            if (_clientRoomScenes.ContainsKey(roomId))
            {
                NotifySceneLoadedServerRpc(roomId);
                return;
            }

            string sceneToLoad = roomName.Split('_')[0];
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics2D);
            var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, parameters);
            await asyncOp;

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            _clientRoomScenes[roomId] = loadedScene;

            NotifySceneLoadedServerRpc(roomId);
        }

        [Rpc(SendTo.Server)]
        private void NotifySceneLoadedServerRpc(NetworkGuid roomId, RpcParams rpcParams = default) { }

        private void HandlePlayerExit(NetworkGuid instanceId, ulong clientId)
        {
            if (_playerToRoomMap.ContainsKey(clientId))
                _playerToRoomMap.Remove(clientId);

            int index = GetRoomIndexByInstanceId(instanceId);
            if (index == -1) return;

            int remaining = 0;
            foreach (var kv in _playerToRoomMap)
            {
                if (kv.Value.Equals(instanceId)) remaining++;
            }

            var room = ActiveRooms[index];
            room.CurrentPlayers = remaining;
            if (room.CurrentPlayers <= 0)
            {
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
                if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
                _clientRoomScenes.Remove(instanceId);
            }

            _signalBus.Fire(new PlayerLeftRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId.ToString()));
        }

        public void RequestCreateRoom(FixedString128Bytes roomName, int maxPlayers) => CreateRoomServerRpc(roomName, maxPlayers);
        public void RequestJoinRoom(NetworkGuid instanceId) => JoinRoomServerRpc(instanceId);
        public void RequestLeaveRoom(NetworkGuid instanceId) => LeaveRoomServerRpc(instanceId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void CreateRoomServerRpc(FixedString128Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            NetworkGuid targetInstance = GetOrCreateRoomInternal(roomName, maxPlayers);
            JoinRoomInternal(targetInstance, senderId).Forget();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinRoomServerRpc(NetworkGuid instanceId, RpcParams rpcParams = default)
        {
            JoinRoomInternal(instanceId, rpcParams.Receive.SenderClientId).Forget();
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