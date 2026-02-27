using AvatarChat.Main;
using AvatarChat.Network.Signals;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using UniRx;
using Unity.Netcode;

namespace AvatarChat.Network.Handlers
{
    public class LocalRoomHandler : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;

        private Scene _currentRoom;
        private string _currentInstanceId;

        private void Start()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            _signalBus.GetStream<PlayerJoinedRoomSignal>()
                .Where(sig => sig.ClientId == NetworkManager.Singleton.LocalClientId)
                .Subscribe(OnLocalJoined)
                .AddTo(this);

            _signalBus.GetStream<PlayerLeftRoomSignal>()
                .Where(sig => sig.ClientId == NetworkManager.Singleton.LocalClientId)
                .Subscribe(OnLocalLeft)
                .AddTo(this);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnLocalJoined(PlayerJoinedRoomSignal sig)
        {
            _currentInstanceId = sig.InstanceId;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive) return;
            if (string.IsNullOrEmpty(_currentInstanceId)) return;

            _currentRoom = scene;
        }

        private void OnLocalLeft(PlayerLeftRoomSignal sig)
        {
            if (_currentRoom.IsValid())
            {
                SceneManager.UnloadSceneAsync(_currentRoom);
            }

            _currentInstanceId = null;
            _currentRoom = default;
        }
    }
}
