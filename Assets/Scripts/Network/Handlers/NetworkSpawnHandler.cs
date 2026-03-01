using AvatarChat.Network.Models;
using AvatarChat.Network.Components;
using AvatarChat.Network.Signals;
using AvatarChat.Main;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace AvatarChat.Network.Handlers
{
    public class NetworkSpawnHandler : SubNetworkHandler
    {
        [Inject] private INetworkHandler _networkHandler;
        [Inject] private SignalBus _signalBus;

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

            if (room.IsEmpty) return null;

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

        public GameObject SpawnObject(GameObject prefab, ulong targetClientId, Vector3 position = default, Quaternion rotation = default)
        {
            if (!IsServer) return null;

            var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
            var room = roomHandler.GetRoomByPlayer(targetClientId);

            if (room.IsEmpty) return null;

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

            _signalBus.Fire(new RoomPlayersUpdatedSignal(roomGuid));
        }

        public List<ulong> GetPlayersInRoom(NetworkGuid roomGuid) =>
            _roomToPlayers.TryGetValue(roomGuid, out var players) ? players : new List<ulong>();

        private void OnClientDisconnected(ulong clientId)
        {
            NetworkGuid affectedRoom = default;
            foreach (var pair in _roomToPlayers)
            {
                if (pair.Value.Contains(clientId))
                {
                    affectedRoom = pair.Key;
                    break;
                }
            }

            foreach (var players in _roomToPlayers.Values) players.Remove(clientId);

            if (!affectedRoom.Equals(default))
            {
                _signalBus.Fire(new RoomPlayersUpdatedSignal(affectedRoom));
            }
        }
    }
}