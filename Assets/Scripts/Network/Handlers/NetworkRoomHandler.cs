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
                _playerToRoomMap.Remove(clientId);
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
            string name = roomName.ToString();
            bool hasSuffix = HasSuffix(name);

            int targetRoomIndex = -1;
            NetworkGuid targetInstance = default;

            string baseName = name.Split('_')[0];

            lock (_roomLock)
            {
                if (!hasSuffix)
                {
                    if (_baseNameToInstance.TryGetValue(baseName, out NetworkGuid existingId))
                    {
                        targetInstance = existingId;
                        targetRoomIndex = GetRoomIndexByInstanceId(existingId);
                    }
                }
                else
                {
                    if (_fullNameToInstance.TryGetValue(name, out NetworkGuid existingId))
                    {
                        targetInstance = existingId;
                        targetRoomIndex = GetRoomIndexByInstanceId(existingId);
                    }
                }

                if (targetRoomIndex == -1)
                {
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

                    targetInstance = newInstance;
                    targetRoomIndex = ActiveRooms.Count - 1;
                }
            }

            if (targetRoomIndex >= 0)
            {
                JoinRoomInternal(targetRoomIndex, senderId).Forget();
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

        private void CreateRoomInternal(FixedString128Bytes roomName, int maxPlayers, ulong clientId)
        {
            NetworkGuid instanceId = Guid.NewGuid();
            ActiveRooms.Add(new NetworkRoom
            {
                RoomName = roomName,
                InstanceId = instanceId,
                MaxPlayers = maxPlayers,
                CurrentPlayers = 0,
                SceneLoaded = false
            });
            JoinRoomInternal(ActiveRooms.Count - 1, clientId).Forget();
        }

        private async UniTask JoinRoomInternal(int roomIndex, ulong clientId)
        {
            var room = ActiveRooms[roomIndex];

            if (room.CurrentPlayers >= room.MaxPlayers) return;

            room.CurrentPlayers++;
            ActiveRooms[roomIndex] = room;
            _playerToRoomMap[clientId] = room.InstanceId;

            var spawnHandler = _networkHandler.GetSubHandler<NetworkSpawnHandler>();
            spawnHandler?.TrackPlayerRoom(clientId, room.InstanceId);

            if (!room.SceneLoaded)
            {
                room.SceneLoaded = true;
                ActiveRooms[roomIndex] = room;
                await LoadSceneOnServerAsync(room.RoomName.ToString(), room.InstanceId);
            }
            else if (IsServer)
            {
                while (!_serverRoomScenes.ContainsKey(room.InstanceId))
                    await UniTask.Yield();
            }

            if (IsServer)
                MovePlayerToRoomScene(clientId, room.InstanceId);

            LoadSceneForClientClientRpc(room.RoomName, room.InstanceId, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            _signalBus.Fire(new PlayerJoinedRoomSignal(clientId, room.InstanceId.ToString()));
        }

        private async UniTask LoadSceneOnServerAsync(string fullRoomName, NetworkGuid roomId)
        {
            string sceneToLoad = fullRoomName.Split('_')[0];
            var parameters = new LoadSceneParameters(
                LoadSceneMode.Additive,
                LocalPhysicsMode.Physics2D);

            var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, parameters);
            await asyncOp;

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            _serverRoomScenes[roomId] = loadedScene;
        }

        private void MovePlayerToRoomScene(ulong clientId, NetworkGuid roomId)
        {
            if (_serverRoomScenes.TryGetValue(roomId, out Scene roomScene))
            {
                var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
                if (playerObj != null)
                    SceneManager.MoveGameObjectToScene(playerObj.gameObject, roomScene);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void LoadSceneForClientClientRpc(FixedString128Bytes roomName, NetworkGuid roomId, RpcParams delivery)
        {
            ExecuteClientSceneLoad(roomName.ToString(), roomId).Forget();
        }

        private async UniTaskVoid ExecuteClientSceneLoad(string roomName, NetworkGuid roomId)
        {
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
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId))
                {
                    var room = ActiveRooms[i];
                    room.CurrentPlayers--;

                    if (room.CurrentPlayers <= 0)
                    {
                        RemoveRoom(instanceId);
                    }
                    else
                    {
                        ActiveRooms[i] = room;
                    }
                    break;
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void LeaveRoomServerRpc(NetworkGuid instanceId, RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            HandlePlayerExit(instanceId, clientId);
            _playerToRoomMap.Remove(clientId);

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
            => CreateRoomInternal(roomName, maxPlayers, rpcParams.Receive.SenderClientId);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinRoomServerRpc(NetworkGuid instanceId, RpcParams rpcParams = default)
        {
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId))
                {
                    JoinRoomInternal(i, rpcParams.Receive.SenderClientId).Forget();
                    break;
                }
            }
        }

        public void RemoveRoom(NetworkGuid instanceId)
        {
            if (!IsServer) return;

            if (_serverRoomScenes.TryGetValue(instanceId, out Scene scene))
            {
                if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
                _serverRoomScenes.Remove(instanceId);
            }

            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId))
                {
                    string rn = ActiveRooms[i].RoomName.ToString();
                    if (HasSuffix(rn))
                    {
                        _fullNameToInstance.Remove(rn);
                    }
                    else
                    {
                        string baseName = rn.Split('_')[0];
                        _baseNameToInstance.Remove(baseName);
                    }

                    ActiveRooms.RemoveAt(i);
                    break;
                }
            }
        }

        public NetworkRoom GetRoom(NetworkGuid value)
        {
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(value)) return ActiveRooms[i];
            }
            return NetworkRoom.Empty;
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