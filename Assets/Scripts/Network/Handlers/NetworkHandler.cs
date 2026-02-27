using System;
using Unity.Netcode;
using Zenject;
using AvatarChat.Main;
using AvatarChat.Network.Signals;
using System.Text;

namespace AvatarChat.Network.Handlers
{
    public class NetworkHandler : NetworkBehaviour, INetworkHandler
    {
        [Inject] private SignalBus _signalBus;

        public void StartHost()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += () =>
                _signalBus.Fire(new NetworkStartedSignal(true, true, NetworkManager.Singleton.LocalClientId));

            NetworkManager.Singleton.StartHost();
            SubscribeServerEvents();
        }

        public void StartClient()
        {
            NetworkManager.Singleton.OnClientStarted += () =>
                _signalBus.Fire(new NetworkStartedSignal(false, false, NetworkManager.Singleton.LocalClientId));

            NetworkManager.Singleton.StartClient();
        }

        public void StartServer()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            NetworkManager.Singleton.OnServerStarted += () =>
                _signalBus.Fire(new NetworkStartedSignal(true, false, NetworkManager.Singleton.LocalClientId));

            NetworkManager.Singleton.StartServer();
            SubscribeServerEvents();
        }

        public void Shutdown()
        {
            NetworkManager.Singleton.Shutdown();
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