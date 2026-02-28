using AvatarChat.Main;
using AvatarChat.Network.Models;
using AvatarChat.Network.Signals;
using System;
using System.Collections.Generic;
using UniRx;
using Unity.Collections;
using Unity.Netcode;
using Zenject;
using Sirenix.OdinInspector;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace AvatarChat.Network.Handlers
{
    public class NetworkPlayerHandler : SubNetworkHandler
    {
        [Inject] private SignalBus _signalBus;

        [ShowInInspector, ReadOnly]
        public NetworkList<NetworkPlayer> ConnectedPlayers { get; private set; }
        private readonly Dictionary<ulong, string> _pendingNames = new();

        private void Awake() => ConnectedPlayers = new NetworkList<NetworkPlayer>();

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
                InstanceId = Guid.NewGuid()
            });

            _pendingNames.Remove(clientId);
        }

        private void OnServerClientDisconnected(ulong clientId)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (ConnectedPlayers[i].ClientId == clientId)
                {
                    ConnectedPlayers.RemoveAt(i);
                    break;
                }
            }
            _pendingNames.Remove(clientId);
        }

        [Rpc(SendTo.Server)]
        public void UpdatePlayerInstanceRpc(ulong clientId, NetworkGuid instanceId)
        {
            for (int i = 0; i < ConnectedPlayers.Count; i++)
            {
                if (ConnectedPlayers[i].ClientId == clientId)
                {
                    var player = ConnectedPlayers[i];
                    player.InstanceId = instanceId;
                    ConnectedPlayers[i] = player;
                    break;
                }
            }
        }
    }
}