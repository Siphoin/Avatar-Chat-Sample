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

namespace AvatarChat.Network.Handlers
{
    public class NetworkRoomHandler : SubNetworkHandler, INetworkRoomHandler
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private INetworkHandler _networkHandler;

        public NetworkList<NetworkRoom> ActiveRooms { get; private set; }
        private Dictionary<ulong, NetworkGuid> _playerToRoomMap = new();

        private string _expectedSceneName;

        private void Awake() => ActiveRooms = new();

        public override void OnNetworkSpawn()
        {
            NetworkManager.Singleton.SceneManager.VerifySceneBeforeLoading = VerifyScene;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnect;
                NetworkManager.Singleton.SceneManager.OnLoadComplete += OnServerSceneLoadComplete;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnect;
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnServerSceneLoadComplete;
            }
        }

        private void OnServerSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadMode)
        {
            if (!IsServer) return;
            Debug.Log($"[NetworkRoomHandler] Scene loaded: {sceneName} for client: {clientId}");
        }

        private void OnServerClientDisconnect(ulong clientId)
        {
            if (_playerToRoomMap.TryGetValue(clientId, out NetworkGuid instanceId))
            {
                Debug.Log($"[NetworkRoomHandler] Client {clientId} disconnected. Cleaning up room {instanceId}");
                HandlePlayerExit(instanceId, clientId);
                _playerToRoomMap.Remove(clientId);
            }
        }

        private bool VerifyScene(int sceneIndex, string sceneName, LoadSceneMode loadMode)
        {
            if (IsServer) return true;
            bool isExpected = sceneName == _expectedSceneName;
            Debug.Log($"[NetworkRoomHandler] Verifying scene {sceneName}: {isExpected}");
            return isExpected;
        }

        public void RequestJoinOrCreateRoom(FixedString128Bytes roomName, int maxPlayers)
        {
            Debug.Log($"[NetworkRoomHandler] Requesting JoinOrCreate: {roomName}");
            JoinOrCreateRoomServerRpc(roomName, maxPlayers);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinOrCreateRoomServerRpc(FixedString128Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[NetworkRoomHandler] Server received JoinOrCreate for {roomName} from {senderId}");

            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].RoomName == roomName)
                {
                    Debug.Log($"[NetworkRoomHandler] Room exists. Joining index {i}");
                    JoinRoomInternal(i, senderId);
                    return;
                }
            }

            Debug.Log($"[NetworkRoomHandler] Room not found. Creating and joining...");
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
                CurrentPlayers = 0
            });

            Debug.Log($"[NetworkRoomHandler] Room created with ID {instanceId}. Triggering entry for {clientId}");
            JoinRoomInternal(ActiveRooms.Count - 1, clientId);
        }

        private void JoinRoomInternal(int roomIndex, ulong clientId)
        {
            var room = ActiveRooms[roomIndex];
            if (room.CurrentPlayers >= room.MaxPlayers)
            {
                Debug.LogWarning($"[NetworkRoomHandler] Join failed: Room {room.RoomName} is full.");
                return;
            }

            room.CurrentPlayers++;
            ActiveRooms[roomIndex] = room;

            // Сначала обновляем мапу, чтобы GetRoomByPlayer работал корректно
            _playerToRoomMap[clientId] = room.InstanceId;

            // Принудительно регистрируем игрока в SpawnHandler для немедленного доступа
            var spawnHandler = _networkHandler.GetSubHandler<NetworkSpawnHandler>();
            if (spawnHandler != null)
            {
                spawnHandler.TrackPlayerRoom(clientId, room.InstanceId);
            }

            Debug.Log($"[NetworkRoomHandler] Joining client {clientId} to room {room.RoomName}. Players: {room.CurrentPlayers}");

            PrepareClientForSceneLoadClientRpc(room.RoomName, RpcTarget.Single(clientId, RpcTargetUse.Temp));

            var status = NetworkManager.Singleton.SceneManager.LoadScene(room.RoomName.ToString(), LoadSceneMode.Additive);

            // Сигнал отправляем в последнюю очередь
            _signalBus.Fire(new PlayerJoinedRoomSignal(clientId, room.InstanceId.ToString()));

            ConfirmActionClientRpc(room.InstanceId, room.RoomName, true, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        private void HandlePlayerExit(NetworkGuid instanceId, ulong clientId)
        {
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId))
                {
                    var room = ActiveRooms[i];
                    room.CurrentPlayers--;
                    Debug.Log($"[NetworkRoomHandler] Player {clientId} left. Remaining: {room.CurrentPlayers}");

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

            ConfirmActionClientRpc(instanceId, "", false, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void PrepareClientForSceneLoadClientRpc(FixedString128Bytes sceneName, RpcParams delivery)
        {
            Debug.Log($"[NetworkRoomHandler] Client preparing for scene: {sceneName}");
            _expectedSceneName = sceneName.ToString();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ConfirmActionClientRpc(NetworkGuid instanceId, FixedString128Bytes sceneName, bool isJoin, RpcParams delivery)
        {
            Debug.Log($"[NetworkRoomHandler] Client ConfirmAction: Join={isJoin}, RoomID={instanceId}");
            if (isJoin)
            {
                Debug.Log($"[NetworkRoomHandler] Firing PlayerJoinedRoomSignal for {instanceId}");
                _signalBus.Fire(new PlayerJoinedRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId.ToString()));
            }
            else
            {
                LeaveRoomLocalCleanup(instanceId.ToString());
            }
        }

        private void LeaveRoomLocalCleanup(string instanceId)
        {
            if (!string.IsNullOrEmpty(_expectedSceneName))
            {
                var scene = SceneManager.GetSceneByName(_expectedSceneName);
                if (scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
            }

            _expectedSceneName = null;
            _signalBus.Fire(new PlayerLeftRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId));
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
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(instanceId))
                {
                    var sceneName = ActiveRooms[i].RoomName.ToString();
                    Debug.Log($"[NetworkRoomHandler] Removing room {sceneName}");
                    ActiveRooms.RemoveAt(i);

                    var scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.isLoaded) NetworkManager.Singleton.SceneManager.UnloadScene(scene);
                    break;
                }
            }
        }

        public NetworkRoom GetRoom(NetworkGuid value)
        {
            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId.Equals(value))
                {
                    return ActiveRooms[i];
                }
            }

            return NetworkRoom.Empty;
        }

        internal NetworkRoom GetRoomByPlayer(ulong ownerClientId)
        {
            if (_playerToRoomMap.TryGetValue(ownerClientId, out NetworkGuid instanceId))
            {
                return GetRoom(instanceId);
            }
            return NetworkRoom.Empty;
        }
    }
}