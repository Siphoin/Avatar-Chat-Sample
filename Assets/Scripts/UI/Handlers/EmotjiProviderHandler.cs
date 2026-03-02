using Assets.Scripts.Meta.SO;
using AvatarChat.Main;
using AvatarChat.Network.Models;
using AvatarChat.Network.Signals;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace AvatarChat.UI.Handlers
{
    public class EmotjiProviderHandler : MonoBehaviour, IDisposable, IEmotjiProviderHandler
    {
        [Inject] private EmotjiContainer _container;
        [Inject] private SignalBus _signalBus;

        private readonly Dictionary<NetworkGuid, Sprite> _loadedSprites = new();
        private readonly CompositeDisposable _disposables = new();

        public void Start()
        {
            _signalBus.GetStream<DestroyChatMessageSignal>()
                .Subscribe(OnMessageDestroyed)
                .AddTo(_disposables);
        }

        public async UniTask<Sprite> GetSpriteForMessage(NetworkMessage message)
        {
            if (message.Type != MessageType.Emoji) return null;

            int emojiId = BitConverter.ToInt32(message.Data, 0);

            Sprite sprite = await _container.LoadEmotji(emojiId);

            if (sprite != null)
            {
                _loadedSprites[message.InstanceId] = sprite;
            }

            return sprite;
        }

        private void OnMessageDestroyed(DestroyChatMessageSignal signal)
        {
            if (_loadedSprites.TryGetValue(signal.Message.InstanceId, out Sprite sprite))
            {
                _container.ReleaseEmotji(sprite);
                _loadedSprites.Remove(signal.Message.InstanceId);
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}