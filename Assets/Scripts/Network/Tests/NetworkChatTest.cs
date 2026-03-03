using AvatarChat.Main;
using AvatarChat.Network.Handlers;
using AvatarChat.Network.Signals;
using Unity.Netcode;
using UnityEngine;
using Zenject;
using UniRx;
using System.Linq;

namespace AvatarChat.Network.Test
{
    public class NetworkChatTest : MonoBehaviour
    {
        [Inject] private INetworkHandler _networkHandler;
        [Inject] private SignalBus _signalBus;

        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=";

        private void Start()
        {
            _signalBus.GetStream<NewChatMessageSignal>()
                .Subscribe(sig =>
                {
                    string content = sig.Message.Type == Models.MessageType.Text
                        ? System.Text.Encoding.UTF8.GetString(sig.Message.Data)
                        : $"Emoji ID/Garbage: {System.BitConverter.ToInt32(sig.Message.Data, 0)}";

                    Debug.Log($"<color=green>[Chat Test]</color> New Message from {sig.Message.OwnerClientId}: {content}");
                })
                .AddTo(this);

            _signalBus.GetStream<DestroyChatMessageSignal>()
                .Subscribe(sig =>
                {
                    Debug.Log($"<color=red>[Chat Test]</color> Message from {sig.Message.OwnerClientId} (ID: {sig.Message.InstanceId}) removed (limit or timeout).");
                })
                .AddTo(this);
        }

        private void Update()
        {
            var chatHandler = _networkHandler.GetSubHandler<NetworkChatHandler>();
            if (chatHandler == null) return;

            if (Input.GetKeyDown(KeyCode.T))
            {
                string randomText = GenerateRandomString(Random.Range(5, 15));
                chatHandler.SendTextMessage(randomText);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                int garbageValue = Random.Range(0, 42);
                chatHandler.SendEmojiMessage(garbageValue);
            }
        }

        private string GenerateRandomString(int length)
        {
            return new string(Enumerable.Repeat(Chars, length)
                .Select(s => s[Random.Range(0, s.Length)]).ToArray());
        }
    }
}