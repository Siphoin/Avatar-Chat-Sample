using AvatarChat.Network.Handlers;
using AvatarChat.Network.Signals;
using AvatarChat.Core.Configs;
using Unity.Netcode;
using UnityEngine;
using Zenject;
using UniRx;
using AvatarChat.Main;

namespace AvatarChat.Core.Handlers
{
    public class PlayerSpawnerHandler : SubNetworkHandler
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private PlayerSpawnerHandlerConfig _config;
        [Inject] private INetworkHandler _networkHandler;

        private CompositeDisposable _disposables = new();

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            _signalBus.GetStream<PlayerJoinedRoomSignal>()
                .Subscribe(OnPlayerJoinedRoom)
                .AddTo(_disposables);

            _signalBus.GetStream<PlayerLeftRoomSignal>()
                .Subscribe(OnPlayerLeftRoom)
                .AddTo(_disposables);
        }

        public override void OnNetworkDespawn()
        {
            _disposables.Dispose();
        }

        private void OnPlayerJoinedRoom(PlayerJoinedRoomSignal signal)
        {
            var spawnHandler = _networkHandler.GetSubHandler<NetworkSpawnHandler>();

            spawnHandler.RequestSpawnObject(
                _config.PlayerPrefab.gameObject,
                signal.ClientId,
                Vector3.zero,
                Quaternion.identity
            );
        }

        private void OnPlayerLeftRoom(PlayerLeftRoomSignal signal)
        {
            if (!IsServer) return;

            var connectedClients = NetworkManager.Singleton.ConnectedClients;
            if (!connectedClients.TryGetValue(signal.ClientId, out var client))
                return;

            var ownedObjects = NetworkManager.Singleton.SpawnManager.GetClientOwnedObjects(signal.ClientId);
            foreach (var networkObj in ownedObjects)
            {
                if (networkObj != null && networkObj.IsSpawned)
                {
                    networkObj.Despawn(true);
                }
            }
        }
    }
}