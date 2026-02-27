using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
            SetupAndStart(() => NetworkManager.Singleton.StartHost(), true, true);
        }

        public void StartClient()
        {
            SetupAndStart(() => NetworkManager.Singleton.StartClient(), false, false);
        }

        public void StartServer()
        {
            SetupAndStart(() => NetworkManager.Singleton.StartServer(), true, false);
        }

        private void SetupAndStart(Func<bool> startAction, bool isServer, bool isHost)
        {
            EnsureNetworkManagerExists();
            ApplyTransportSettings();

            if (isServer) NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

            if (startAction.Invoke())
            {
                _signalBus.Fire(new NetworkStartedSignal(isServer, isHost, NetworkManager.Singleton.LocalClientId));
                if (isServer) SubscribeServerEvents();
            }
        }

        private void ApplyTransportSettings()
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = _config.Address;
                transport.ConnectionData.Port = _config.Port;
            }
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
            NetworkManager.Singleton.OnClientConnectedCallback += (id) => _signalBus.Fire(new PlayerJoinedSignal(id));
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) => _signalBus.Fire(new PlayerLeftSignal(id));
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
            response.Pending = false;
        }
    }
}