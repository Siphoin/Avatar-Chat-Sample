using UnityEngine;
using Zenject;
using UniRx;
using AvatarChat.Network.Handlers;
using AvatarChat.Network.Signals;
using AvatarChat.Main;

namespace AvatarChat.Tests
{
    public class NetworkBehaviorTest : MonoBehaviour
    {
        [Inject] private INetworkHandler _networkHandler;
        [Inject] private SignalBus _signalBus;

        private void Start()
        {
            _signalBus.GetStream<NetworkStartedSignal>()
                .Subscribe(signal =>
                {
                    string role = signal.IsHost ? "Host" : signal.IsServer ? "Server" : "Client";
                    Debug.Log($"[Test] Network Started. Role: {role}, ID: {signal.LocalClientId}");
                })
                .AddTo(this);

            _signalBus.GetStream<PlayerJoinedSignal>()
                .Subscribe(signal =>
                {
                    Debug.Log($"[Test] Player joined with ID: {signal.ClientId}");
                })
                .AddTo(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H)) _networkHandler.StartHost();
            if (Input.GetKeyDown(KeyCode.C)) _networkHandler.StartClient();
            if (Input.GetKeyDown(KeyCode.S)) _networkHandler.StartServer();
            if (Input.GetKeyDown(KeyCode.Q)) _networkHandler.Shutdown();
        }
    }
}