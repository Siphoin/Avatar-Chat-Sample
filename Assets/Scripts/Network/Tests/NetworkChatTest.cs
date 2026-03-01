using AvatarChat.Main;
using AvatarChat.Network.Handlers;
using AvatarChat.Network.Signals;
using Unity.Netcode;
using UnityEngine;
using Zenject;
using UniRx;

namespace AvatarChat.Network.Test
{
    public class NetworkChatTest : MonoBehaviour
    {
        [Inject] private INetworkHandler _networkHandler;
        [Inject] private SignalBus _signalBus;

        private void Start()
        {
            _signalBus.GetStream<NewChatMessageSignal>()
                .Subscribe(sig =>
                {
                    string content = sig.Message.Type == Models.MessageType.Text
                        ? System.Text.Encoding.UTF8.GetString(sig.Message.Data)
                        : $"Emoji ID: {System.BitConverter.ToInt32(sig.Message.Data, 0)}";

                    Debug.Log($"[Chat Test] New Message from {sig.Message.OwnerClientId}: {content}");
                })
                .AddTo(this);

            _signalBus.GetStream<DestroyChatMessageSignal>()
                .Subscribe(sig =>
                {
                    Debug.Log($"[Chat Test] Message from {sig.Message.OwnerClientId} destroyed by timeout.");
                })
                .AddTo(this);
        }

        private void Update()
        {
            var chatHandler = _networkHandler.GetSubHandler<NetworkChatHandler>();
            if (chatHandler == null) return;

            if (Input.GetKeyDown(KeyCode.T))
            {
                chatHandler.SendTextMessage($"Hello from {NetworkManager.Singleton.LocalClientId} at {Time.time}");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                int randomEmojiId = Random.Range(1, 10);
                chatHandler.SendEmojiMessage(randomEmojiId);
            }
        }
    }
}