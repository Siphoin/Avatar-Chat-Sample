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
    }
}