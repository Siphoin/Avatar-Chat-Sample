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
using UniRx;

namespace AvatarChat.Network.Handlers
{
    public class NetworkChatHandler : SubNetworkHandler, IDisposable
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private NetworkChatHandlerConfig _config;

        private readonly Dictionary<NetworkGuid, List<NetworkMessage>> _roomMessages = new();
        private readonly Dictionary<ulong, NetworkGuid> _playerRooms = new();
        private readonly List<NetworkMessage> _localClientMessages = new();

        private readonly Dictionary<NetworkGuid, float> _messageCreationTimes = new();
        private readonly CompositeDisposable _disposables = new();

        public void Start()
        {
            _signalBus.GetStream<PlayerJoinedRoomSignal>()
                .Subscribe(OnPlayerJoined)
                .AddTo(_disposables);

            _signalBus.GetStream<PlayerLeftSignal>()
                .Subscribe(sig => _playerRooms.Remove(sig.ClientId))
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private void OnPlayerJoined(PlayerJoinedRoomSignal signal)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            var roomGuid = new NetworkGuid(new Guid(signal.InstanceId));
            _playerRooms[signal.ClientId] = roomGuid;

            if (!_roomMessages.TryGetValue(roomGuid, out var messages)) return;

            foreach (var message in messages.ToList())
            {
                if (!_messageCreationTimes.TryGetValue(message.InstanceId, out float createdAt)) continue;

                float elapsed = UnityEngine.Time.time - createdAt;
                float remainingLife = _config.LifeStyleMessage - elapsed;

                if (remainingLife <= 0) continue;

                var rpcParams = new RpcParams
                {
                    Send = new RpcSendParams
                    {
                        Target = NetworkManager.Singleton.RpcTarget.Single(signal.ClientId, RpcTargetUse.Temp)
                    }
                };

                SendHistoryMessageClientRpc(message, remainingLife, rpcParams);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SendHistoryMessageClientRpc(NetworkMessage message, float remainingLife, RpcParams rpcParams)
        {
            if (_localClientMessages.Any(m => m.InstanceId.Equals(message.InstanceId))) return;
            ProcessNewMessage(message, remainingLife);
        }

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
            if (!_playerRooms.TryGetValue(senderId, out var roomId)) return;

            if (!_roomMessages.ContainsKey(roomId)) _roomMessages[roomId] = new List<NetworkMessage>();
            var messages = _roomMessages[roomId];

            var playerMessages = messages.Where(m => m.OwnerClientId == senderId).ToList();
            if (playerMessages.Count >= _config.MaxMessageByPlayer)
            {
                var oldestMessage = playerMessages.First();
                BroadcastDestroyInRoom(roomId, oldestMessage.InstanceId);
                RemoveAndDestroyInternal(oldestMessage, roomId);
            }

            var message = new NetworkMessage
            {
                OwnerClientId = senderId,
                Type = type,
                Data = data,
                InstanceId = new(Guid.NewGuid()),
            };

            ProcessNewMessage(message, _config.LifeStyleMessage, roomId);
            BroadcastToRoom(roomId, message);
        }

        private void BroadcastToRoom(NetworkGuid roomId, NetworkMessage message)
        {
            var targetIds = _playerRooms
                .Where(kvp => kvp.Value.Equals(roomId))
                .Select(kvp => kvp.Key)
                .ToArray();

            var rpcParams = new RpcParams
            {
                Send = new RpcSendParams
                {
                    Target = NetworkManager.Singleton.RpcTarget.Group(targetIds, RpcTargetUse.Temp)
                }
            };

            ReceiveMessageClientRpc(message, rpcParams);
        }

        private void BroadcastDestroyInRoom(NetworkGuid roomId, NetworkGuid msgId)
        {
            var targetIds = _playerRooms
                .Where(kvp => kvp.Value.Equals(roomId))
                .Select(kvp => kvp.Key)
                .ToArray();

            var rpcParams = new RpcParams
            {
                Send = new RpcSendParams
                {
                    Target = NetworkManager.Singleton.RpcTarget.Group(targetIds, RpcTargetUse.Temp)
                }
            };

            DestroyMessageClientRpc(msgId, rpcParams);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ReceiveMessageClientRpc(NetworkMessage message, RpcParams rpcParams = default)
        {
            if (_localClientMessages.Any(m => m.InstanceId.Equals(message.InstanceId))) return;
            ProcessNewMessage(message, _config.LifeStyleMessage);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void DestroyMessageClientRpc(NetworkGuid instanceId, RpcParams rpcParams = default)
        {
            var messageToRemove = _localClientMessages.FirstOrDefault(m => m.InstanceId.Equals(instanceId));
            if (!messageToRemove.InstanceId.Equals(default))
            {
                RemoveAndDestroyInternal(messageToRemove);
            }
        }

        private void ProcessNewMessage(NetworkMessage message, float lifeTime, NetworkGuid roomId = default)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (!roomId.Equals(default))
                {
                    if (!_roomMessages.ContainsKey(roomId)) _roomMessages[roomId] = new List<NetworkMessage>();
                    _roomMessages[roomId].Add(message);
                }
                _messageCreationTimes[message.InstanceId] = UnityEngine.Time.time;
            }
            else
            {
                _localClientMessages.Add(message);
            }

            _signalBus.Fire(new NewChatMessageSignal(message));
            HandleMessageLifecycle(message, lifeTime, roomId).Forget();
        }

        private async UniTaskVoid HandleMessageLifecycle(NetworkMessage message, float lifeTime, NetworkGuid roomId)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(lifeTime));
            RemoveAndDestroyInternal(message, roomId);
        }

        private void RemoveAndDestroyInternal(NetworkMessage message, NetworkGuid roomId = default)
        {
            bool removed = false;
            if (NetworkManager.Singleton.IsServer)
            {
                if (_roomMessages.TryGetValue(roomId, out var messages))
                    removed = messages.Remove(message);

                _messageCreationTimes.Remove(message.InstanceId);
            }
            else
            {
                removed = _localClientMessages.Remove(message);
            }

            if (removed)
            {
                _signalBus.Fire(new DestroyChatMessageSignal(message));
            }
        }

        public IReadOnlyList<NetworkMessage> GetHistory()
            => NetworkManager.Singleton.IsServer ? new List<NetworkMessage>() : _localClientMessages;
    }
}