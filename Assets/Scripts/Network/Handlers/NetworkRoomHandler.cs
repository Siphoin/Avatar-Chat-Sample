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

        private bool VerifyScene(int sceneIndex, string sceneName, LoadSceneMode loadMode)
        {
            if (IsServer) return true;
            return sceneName == _expectedSceneName;
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
                CurrentPlayers = 1
            });

            _playerToRoomMap[clientId] = instanceId;

            PrepareClientForSceneLoadClientRpc(roomName, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            NetworkManager.Singleton.SceneManager.LoadScene(roomName.ToString(), LoadSceneMode.Additive);
            ConfirmActionClientRpc(instanceId, roomName, true, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        private void JoinRoomInternal(int roomIndex, ulong clientId)
        {
            var room = ActiveRooms[roomIndex];
            if (room.CurrentPlayers >= room.MaxPlayers) return;

            room.CurrentPlayers++;
            ActiveRooms[roomIndex] = room;

            _playerToRoomMap[clientId] = room.InstanceId;

            PrepareClientForSceneLoadClientRpc(room.RoomName, RpcTarget.Single(clientId, RpcTargetUse.Temp));
            NetworkManager.Singleton.SceneManager.LoadScene(room.RoomName.ToString(), LoadSceneMode.Additive);
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
            _expectedSceneName = sceneName.ToString();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ConfirmActionClientRpc(NetworkGuid instanceId, FixedString128Bytes sceneName, bool isJoin, RpcParams delivery)
        {
            if (isJoin)
            {
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
                    ActiveRooms.RemoveAt(i);

                    var scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.isLoaded) NetworkManager.Singleton.SceneManager.UnloadScene(scene);
                    break;
                }
            }
        }
    }
}