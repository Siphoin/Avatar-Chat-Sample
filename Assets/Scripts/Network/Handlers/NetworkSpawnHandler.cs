using AvatarChat.Network.Models;
using AvatarChat.Network.Components;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace AvatarChat.Network.Handlers
{
    public class NetworkSpawnHandler : SubNetworkHandler
    {
        [Inject] private INetworkHandler _networkHandler;

        private readonly Dictionary<NetworkGuid, List<ulong>> _roomToPlayers = new();

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        public GameObject RequestSpawnObject(GameObject prefab, ulong targetClientId, Vector3 position = default, Quaternion rotation = default)
        {
            if (!IsServer) return null;

            var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
            var room = roomHandler.GetRoomByPlayer(targetClientId);

            if (room.IsEmpty)
            {
                Debug.LogError($"[NetworkSpawnHandler] Cannot spawn: Player {targetClientId} is not registered in any room!");
                return null;
            }

            var instance = Instantiate(prefab, position, rotation);
            var netObj = instance.GetComponent<Unity.Netcode.NetworkObject>();

            if (netObj != null)
            {
                netObj.SpawnWithOwnership(targetClientId);
            }

            var customNetObj = instance.GetComponent<AvatarChat.Network.Components.NetworkObject>();
            if (customNetObj != null)
            {
                customNetObj.SetRoom(room);
                customNetObj.CheckObjectVisibility();
            }

            return instance;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SpawnObjectServerRpc(uint prefabHash, Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;

            var prefabsList = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs;

            foreach (var networkPrefab in prefabsList)
            {
                if (networkPrefab.Prefab.GetComponent<Unity.Netcode.NetworkObject>().PrefabIdHash == prefabHash)
                {
                    SpawnObject(networkPrefab.Prefab, senderId, position, rotation);
                    return;
                }
            }

            Debug.LogWarning($"Prefab with hash {prefabHash} not found in NetworkManager prefabs list.");
        }

        public GameObject SpawnObject(GameObject prefab, ulong targetClientId, Vector3 position = default, Quaternion rotation = default)
        {
            if (!IsServer) return null;

            var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
            var room = roomHandler.GetRoomByPlayer(targetClientId);

            if (room.IsEmpty)
            {
                Debug.LogError($"[NetworkSpawnHandler] Cannot spawn: Player {targetClientId} is not registered in any room!");
                return null;
            }

            var instance = Instantiate(prefab, position, rotation);
            var netObj = instance.GetComponent<Unity.Netcode.NetworkObject>();

            if (netObj != null)
            {
                netObj.SpawnWithOwnership(targetClientId);
            }

            var customNetObj = instance.GetComponent<AvatarChat.Network.Components.NetworkObject>();
            if (customNetObj != null)
            {
                customNetObj.SetRoom(room);
                customNetObj.CheckObjectVisibility();
            }

            return instance;
        }

        public void TrackPlayerRoom(ulong clientId, NetworkGuid roomGuid)
        {
            if (!IsServer) return;
            foreach (var players in _roomToPlayers.Values) players.Remove(clientId);

            if (!_roomToPlayers.ContainsKey(roomGuid))
                _roomToPlayers[roomGuid] = new List<ulong>();

            _roomToPlayers[roomGuid].Add(clientId);
        }

        public List<ulong> GetPlayersInRoom(NetworkGuid roomGuid) =>
            _roomToPlayers.TryGetValue(roomGuid, out var players) ? players : new List<ulong>();

        private void OnClientDisconnected(ulong clientId)
        {
            foreach (var players in _roomToPlayers.Values) players.Remove(clientId);
        }
    }
}