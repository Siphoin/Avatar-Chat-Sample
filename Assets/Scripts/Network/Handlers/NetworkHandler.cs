using System;
using Unity.Netcode;
using Zenject;
using AvatarChat.Main;
using AvatarChat.Network.Signals;
using AvatarChat.Network.Configs;
using UnityEngine;

namespace AvatarChat.Network.Handlers
{
    public class NetworkHandler : MonoBehaviour, INetworkHandler
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private NetworkHandlerConfig _config;

        public void StartHost()
        {
            EnsureNetworkManagerExists();
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += () =>
                _signalBus.Fire(new NetworkStartedSignal(true, true, NetworkManager.Singleton.LocalClientId));

            NetworkManager.Singleton.StartHost();
            SubscribeServerEvents();
        }

        public void StartClient()
        {
            EnsureNetworkManagerExists();
            NetworkManager.Singleton.OnClientStarted += () =>
                _signalBus.Fire(new NetworkStartedSignal(false, false, NetworkManager.Singleton.LocalClientId));

            NetworkManager.Singleton.StartClient();
        }

        public void StartServer()
        {
            EnsureNetworkManagerExists();
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += () =>
                _signalBus.Fire(new NetworkStartedSignal(true, false, NetworkManager.Singleton.LocalClientId));

            NetworkManager.Singleton.StartServer();
            SubscribeServerEvents();
        }

        public void Shutdown()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                Destroy(NetworkManager.Singleton.gameObject);
            }
        }

        private void EnsureNetworkManagerExists()
        {
            if (NetworkManager.Singleton != null) return;

            Instantiate(_config.NetworkManagerPrefab);
        }

        private void SubscribeServerEvents()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
                _signalBus.Fire(new PlayerJoinedSignal(id));

            NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
                _signalBus.Fire(new PlayerLeftSignal(id));
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Pending = false;
        }
    }
}