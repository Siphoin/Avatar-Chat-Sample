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
            _signalBus.GetStream<RoomCreatedSignal>()
                .Subscribe(OnRoomCreated)
                .AddTo(this);

            _signalBus.GetStream<PlayerJoinedRoomSignal>()
                .Subscribe(sig => Debug.Log($"Signal: Player {sig.ClientId} is in {sig.InstanceId}"))
                .AddTo(this);
        }

        private void OnRoomCreated(RoomCreatedSignal signal)
        {
            var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
            if (roomHandler != null)
            {
                roomHandler.JoinRoom(signal.InstanceId);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6) && NetworkManager.Singleton.IsServer)
            {
                var roomHandler = _networkHandler.GetSubHandler<NetworkRoomHandler>();
                roomHandler?.CreateRoom(_testSceneName, 10);
            }
        }
    }
}