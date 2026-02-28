using AvatarChat.Main;
using AvatarChat.Network.Models;
using AvatarChat.Network.Signals;
using System;
using System.Collections.Generic;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using Zenject;

namespace AvatarChat.Network.Handlers
{
    public class NetworkPlayerHandler : SubNetworkHandler
    {
        [Inject] private SignalBus _signalBus;

        public NetworkList<NetworkPlayer> ConnectedPlayers { get; private set; }
        private readonly Dictionary<ulong, string> _pendingNames = new();

        private void Awake() => ConnectedPlayers = new();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _signalBus.GetStream<ConnectionApprovedSignal>()
                    .Subscribe(sig => _pendingNames[sig.ClientId] = sig.PlayerName)
                    .AddTo(this);

                NetworkManager.Singleton.OnClientConnectedCallback += OnServerClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnServerClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnServerClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnServerClientDisconnected;
            }
        }

        private void OnServerClientConnected(ulong clientId)
        {
            _pendingNames.TryGetValue(clientId, out string playerName);

            ConnectedPlayers.Add(new NetworkPlayer
            {
                Name = playerName ?? $"Player {clientId}",
                ClientId = clientId,
                InstanceId = Guid.NewGuid().ToString()
            });

            _pendingNames.Remove(clientId);
        }

        private void OnServerClientDisconnected(ulong clientId)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (_pendingNames.ContainsKey(clientId))
                {
                    ConnectedPlayers.RemoveAt(i);
                    break;
                }
            }
            _pendingNames.Remove(clientId);
        }

        [Rpc(SendTo.Server)]
        public void UpdatePlayerInstanceRpc(ulong clientId, FixedString64Bytes instanceId)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                var player = ConnectedPlayers[i];
                player.InstanceId = instanceId;
                ConnectedPlayers[i] = player;
            }
        }
    }
}