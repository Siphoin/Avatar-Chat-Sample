using System;
using System.Collections.Generic;
using AvatarChat.Network.Models;
using Unity.Netcode;
using AvatarChat.Main;
using AvatarChat.Network.Signals;
using Zenject;
using Cysharp.Threading.Tasks;

namespace AvatarChat.Network.Handlers
{
    public class NetworkChatHandler : SubNetworkHandler
    {
        [Inject] private SignalBus _signalBus;

        private readonly List<NetworkMessage> _messages = new();
        private const int MessageLifeTimeSeconds = 10;

        public void SendTextMessage(string text)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            SendMessageServerRpc(MessageType.Text, data);
        }

        public void SendEmojiMessage(int emojiId)
        {
            byte[] data = BitConverter.GetBytes(emojiId);
            SendMessageServerRpc(MessageType.Emoji, data);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SendMessageServerRpc(MessageType type, byte[] data, RpcParams rpcParams = default)
        {
            var message = new NetworkMessage
            {
                OwnerClientId = rpcParams.Receive.SenderClientId,
                Type = type,
                Data = data
            };

            ProcessNewMessage(message);
            BroadcastMessageClientRpc(message);
        }

        [Rpc(SendTo.NotServer)]
        private void BroadcastMessageClientRpc(NetworkMessage message)
        {
            ProcessNewMessage(message);
        }

        private void ProcessNewMessage(NetworkMessage message)
        {
            _messages.Add(message);
            _signalBus.Fire(new NewChatMessageSignal(message));

            HandleMessageLifecycle(message).Forget();
        }

        private async UniTaskVoid HandleMessageLifecycle(NetworkMessage message)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(MessageLifeTimeSeconds));

            if (_messages.Remove(message))
            {
                _signalBus.Fire(new DestroyChatMessageSignal(message));
            }
        }

        public IReadOnlyList<NetworkMessage> GetHistory() => _messages;
    }
}