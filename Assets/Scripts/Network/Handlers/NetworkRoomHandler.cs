using AvatarChat.Network.Models;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;
using System;
using AvatarChat.Main;
using AvatarChat.Network.Signals;
using Zenject;

namespace AvatarChat.Network.Handlers
{
    public class NetworkRoomHandler : SubNetworkHandler, INetworkRoomHandler
    {
        [Inject] private SignalBus _signalBus;

        public NetworkList<NetworkRoom> ActiveRooms { get; private set; }

        private void Awake()
        {
            ActiveRooms = new();
        }

        public void CreateRoom(FixedString64Bytes roomName, int maxPlayers)
        {
            if (IsServer)
            {
                var instanceId = Guid.NewGuid().ToString();
                var newRoom = new NetworkRoom
                {
                    RoomName = roomName,
                    InstanceId = instanceId,
                    MaxPlayers = maxPlayers,
                    CurrentPlayers = 0
                };
                ActiveRooms.Add(newRoom);

                _signalBus.Fire(new RoomCreatedSignal(instanceId, roomName.ToString()));

                NetworkManager.Singleton.SceneManager.LoadScene(roomName.ToString(), LoadSceneMode.Additive);
            }
        }

        public void RemoveRoom(FixedString64Bytes instanceId)
        {
            if (!IsServer) return;

            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId == instanceId)
                {
                    ActiveRooms.RemoveAt(i);
                    _signalBus.Fire(new RoomRemovedSignal(instanceId.ToString()));
                    break;
                }
            }
        }

        public void JoinRoom(FixedString64Bytes instanceId)
        {
            JoinRoomServerRpc(instanceId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinRoomServerRpc(FixedString64Bytes instanceId, RpcParams rpcParams = default)
        {
            var clientId = rpcParams.Receive.SenderClientId;
            _signalBus.Fire(new PlayerJoinedRoomSignal(clientId, instanceId.ToString()));

            Debug.Log($"[Server] Client {clientId} joined room {instanceId}");
        }
    }
}