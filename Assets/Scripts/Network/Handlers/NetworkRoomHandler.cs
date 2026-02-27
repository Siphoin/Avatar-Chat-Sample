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

        private void Awake() => ActiveRooms = new();

        public void RequestCreateRoom(FixedString64Bytes roomName, int maxPlayers) => CreateRoomServerRpc(roomName, maxPlayers);
        public void RequestJoinRoom(FixedString64Bytes instanceId) => JoinRoomServerRpc(instanceId);
        public void RequestLeaveRoom(FixedString64Bytes instanceId) => LeaveRoomServerRpc(instanceId);

        public void RemoveRoom(FixedString64Bytes instanceId)
        {
            if (!IsServer) return;

            for (int i = 0; i < ActiveRooms.Count; i++)
            {
                if (ActiveRooms[i].InstanceId == instanceId)
                {
                    var sceneName = ActiveRooms[i].RoomName.ToString();
                    ActiveRooms.RemoveAt(i);

                    var scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.isLoaded)
                    {
                        NetworkManager.Singleton.SceneManager.UnloadScene(scene);
                    }
                    break;
                }
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void CreateRoomServerRpc(FixedString64Bytes roomName, int maxPlayers, RpcParams rpcParams = default)
        {
            var instanceId = Guid.NewGuid().ToString();
            ActiveRooms.Add(new NetworkRoom { RoomName = roomName, InstanceId = instanceId, MaxPlayers = maxPlayers, CurrentPlayers = 1 });

            NetworkManager.Singleton.SceneManager.LoadScene(roomName.ToString(), LoadSceneMode.Additive);

            ConfirmActionClientRpc(instanceId, roomName, true, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void JoinRoomServerRpc(FixedString64Bytes instanceId, RpcParams rpcParams = default)
        {
            ConfirmActionClientRpc(instanceId, "", true, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void LeaveRoomServerRpc(FixedString64Bytes instanceId, RpcParams rpcParams = default)
        {
            ConfirmActionClientRpc(instanceId, "", false, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ConfirmActionClientRpc(FixedString64Bytes instanceId, FixedString64Bytes sceneName, bool isJoin, RpcParams delivery)
        {
            if (isJoin)
                _signalBus.Fire(new PlayerJoinedRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId.ToString()));
            else
                _signalBus.Fire(new PlayerLeftRoomSignal(NetworkManager.Singleton.LocalClientId, instanceId.ToString()));
        }
    }
}