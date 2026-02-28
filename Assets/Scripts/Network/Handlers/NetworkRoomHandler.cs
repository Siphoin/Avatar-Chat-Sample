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

        private void Awake() => ActiveRooms = new();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnect;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnect;
            }
        }

        private void OnServerClientDisconnect(ulong clientId)
        {
            if (_playerToRoomMap.TryGetValue(clientId, out NetworkGuid instanceId))
            {
                HandlePlayerExit(instanceId, clientId);
                _playerToRoomMap.Remove(clientId);
            }
        }

        public void RequestJoinOrCreateRoom(FixedString128Bytes roomName, int maxPlayers)
        {
            JoinOrCreateRoomServerRpc(roomName, maxPlayers);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinOrCreateRoomServerRpc(FixedString128Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;

            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].RoomName == roomName)
                {
                    JoinRoomInternal(i, senderId);
                    return;
                }
            }

            CreateRoomInternal(roomName, maxPlayers, senderId);
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

            JoinRoomInternal(ActiveRooms.Count - 1, clientId);
        }

        private void JoinRoomInternal(int roomIndex, ulong clientId)
        {
            var room = ActiveRooms[roomIndex];

            if (room.CurrentPlayers >= room.MaxPlayers)
            {
                return;
            }

            room.CurrentPlayers++;
            ActiveRooms[roomIndex] = room;
            _playerToRoomMap[clientId] = room.InstanceId;

            var spawnHandler = _networkHandler.GetSubHandler<NetworkSpawnHandler>();
            spawnHandler?.TrackPlayerRoom(clientId, room.InstanceId);

            if (!room.SceneLoaded)
            {
                LoadSceneOnServerAsync(room.RoomName.ToString(), room.InstanceId).Forget();
                room.SceneLoaded = true;
                ActiveRooms[roomIndex] = room;
            }

            LoadSceneForClientClientRpc(room.RoomName, room.InstanceId, RpcTarget.Single(clientId, RpcTargetUse.Temp));

            _signalBus.Fire(new PlayerJoinedRoomSignal(clientId, room.InstanceId.ToString()));
        }

        private async UniTask LoadSceneOnServerAsync(string sceneName, NetworkGuid roomId)
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, parameters);
            await asyncOp;

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            _serverRoomScenes[roomId] = loadedScene;
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void LoadSceneForClientClientRpc(FixedString128Bytes sceneName, NetworkGuid roomId, RpcParams delivery)
        {
            ExecuteClientSceneLoad(sceneName.ToString(), roomId).Forget();
        }

        private async UniTaskVoid ExecuteClientSceneLoad(string sceneName, NetworkGuid roomId)
        {
            if (_clientRoomScenes.ContainsKey(roomId))
            {
                NotifySceneLoadedServerRpc(roomId);
                return;
            }

            var parameters = new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D);
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, parameters);

            await asyncOp;

            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
            _clientRoomScenes[roomId] = loadedScene;

            NotifySceneLoadedServerRpc(roomId);
        }

        [Rpc(SendTo.Server)]
        private void NotifySceneLoadedServerRpc(NetworkGuid roomId, RpcParams rpcParams = default)
        {
        }

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
            {
                _signalBus.Fire(new PlayerJoinedRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId.ToString()));
            }
            else
            {
                LeaveRoomLocalCleanup(instanceId);
            }
        }

        private void LeaveRoomLocalCleanup(NetworkGuid instanceId)
        {
            if (_clientRoomScenes.TryGetValue(instanceId, out Scene scene))
            {
                if (scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
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
                    JoinRoomInternal(i, rpcParams.Receive.SenderClientId);
                    break;
                }
            }
        }

        public void RemoveRoom(NetworkGuid instanceId)
        {
            if (!IsServer) return;

            if (_serverRoomScenes.TryGetValue(instanceId, out Scene scene))
            {
                if (scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
                _serverRoomScenes.Remove(instanceId);
            }

            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId))
                {
                    ActiveRooms.RemoveAt(i);
                    break;
                }
            }
        }

        public NetworkRoom GetRoom(NetworkGuid value)
        {
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(value))
                    return ActiveRooms[i];
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
            if (IsServer && _serverRoomScenes.TryGetValue(instanceId, out Scene serverScene))
                return serverScene;

            if (!IsServer && _clientRoomScenes.TryGetValue(instanceId, out Scene clientScene))
                return clientScene;

            return default;
        }
    }
}