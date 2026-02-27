using AvatarChat.Main;
using AvatarChat.Network.Handlers;
using AvatarChat.Network.Signals;
using Unity.Netcode;
using UnityEngine;
using Zenject;
using UniRx;

namespace AvatarChat.Network.Test
{
    public class NetworkRoomTester : MonoBehaviour
    {
        [Inject] private INetworkHandler _networkHandler;
        [Inject] private SignalBus _signalBus;

        [SerializeField] private string _testSceneName = "GameScene";

        private void Start()
        {
            _signalBus.GetStream<PlayerJoinedRoomSignal>()
                .Where(sig => sig.ClientId == NetworkManager.Singleton.LocalClientId)
                .Subscribe(sig => Debug.Log($"[Test] Local Player joined room: {sig.InstanceId}"))
                .AddTo(this);

            _signalBus.GetStream<PlayerLeftRoomSignal>()
                .Where(sig => sig.ClientId == NetworkManager.Singleton.LocalClientId)
                .Subscribe(_ => Debug.Log("Test] Local Player left room"))
                .AddTo(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                _networkHandler.StartHost();
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
                roomHandler?.RequestCreateRoom(_testSceneName, 10);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
                if (roomHandler != null && roomHandler.ActiveRooms.Count > 0)
                {
                    roomHandler.RequestLeaveRoom(roomHandler.ActiveRooms[0].InstanceId);
                }
            }

            if (Input.GetKeyDown(KeyCode.F8) && NetworkManager.Singleton.IsServer)
            {
                var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
                if (roomHandler != null && roomHandler.ActiveRooms.Count > 0)
                {
                    roomHandler.RemoveRoom(roomHandler.ActiveRooms[0].InstanceId);
                }
            }
        }
    }
}