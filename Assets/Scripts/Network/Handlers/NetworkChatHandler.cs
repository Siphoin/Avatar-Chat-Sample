using System;
using System.Collections.Generic;
using System.Linq;
using AvatarChat.Network.Models;
using Unity.Netcode;
using AvatarChat.Main;
using AvatarChat.Network.Signals;
using Zenject;
using Cysharp.Threading.Tasks;
using AvatarChat.Network.Configs;

namespace AvatarChat.Network.Handlers
{
    public class NetworkChatHandler : SubNetworkHandler
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private NetworkChatHandlerConfig _config;

        private readonly List<NetworkMessage> _messages = new();

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
            var senderId = rpcParams.Receive.SenderClientId;

            // Проверка лимита на сервере перед рассылкой
            var playerMessages = _messages.Where(m => m.OwnerClientId == senderId).ToList();
            if (playerMessages.Count >= _config.MaxMessageByPlayer)
            {
                var oldestMessage = playerMessages.First();
                DestroyMessageClientRpc(oldestMessage.InstanceId);
                RemoveAndDestroyInternal(oldestMessage);
            }

            var message = new NetworkMessage
            {
                OwnerClientId = senderId,
                Type = type,
                Data = data,
                InstanceId = new(Guid.NewGuid()),
            };

            ProcessNewMessage(message);
            BroadcastMessageClientRpc(message);
        }

        [Rpc(SendTo.NotServer)]
        private void BroadcastMessageClientRpc(NetworkMessage message)
        {
            ProcessNewMessage(message);
        }

        [Rpc(SendTo.NotServer)]
        private void DestroyMessageClientRpc(NetworkGuid instanceId)
        {
            var messageToRemove = _messages.FirstOrDefault(m => m.InstanceId.Equals(instanceId));
            RemoveAndDestroyInternal(messageToRemove);
        }

        private void ProcessNewMessage(NetworkMessage message)
        {
            _messages.Add(message);
            _signalBus.Fire(new NewChatMessageSignal(message));

            HandleMessageLifecycle(message).Forget();
        }

        private async UniTaskVoid HandleMessageLifecycle(NetworkMessage message)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_config.LifeStyleMessage));
            RemoveAndDestroyInternal(message);
        }

        private void RemoveAndDestroyInternal(NetworkMessage message)
        {
            if (_messages.Remove(message))
            {
                _signalBus.Fire(new DestroyChatMessageSignal(message));
            }
        }

        public IReadOnlyList<NetworkMessage> GetHistory() => _messages;
    }
}