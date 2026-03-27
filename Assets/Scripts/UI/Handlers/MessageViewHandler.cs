using AvatarChat.Core.Components;
using AvatarChat.Main;
using AvatarChat.Network.Models;
using AvatarChat.Network.Signals;
using AvatarChat.UI.Configs;
using AvatarChat.UI.Factories;
using AvatarChat.UI.Views;
using ObjectRepositories.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace AvatarChat.UI.Handlers
{
    public class MessageViewHandler : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private IMessageViewFactory _viewFactory;
        [Inject] private MessageViewHandlerConfig _viewConfig;

        private readonly Dictionary<Guid, IMessageView> _activeViews = new();
        private readonly CompositeDisposable _disposable = new();

        private bool IsServer => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        private void OnEnable()
        {
            if (IsServer) return;

            _signalBus.GetStream<NewChatMessageSignal>()
                .Subscribe(OnNewMessage)
                .AddTo(_disposable);

            _signalBus.GetStream<DestroyChatMessageSignal>()
                .Subscribe(OnDestroyMessage)
                .AddTo(_disposable);
        }

        private void OnDisable()
        {
            foreach (var view in _activeViews.Values)
            {
                if (view != null && view is MonoBehaviour mb && mb != null && mb.gameObject != null)
                {
                    mb.gameObject.SetActive(false);
                }
            }
            _activeViews.Clear();
            _disposable.Clear();
        }

        private void OnNewMessage(NewChatMessageSignal signal)
        {
            if (IsServer) return;

            var message = signal.Message;

            var character = this.FindObjectsOfTypeOnRepository<Character>()
                .FirstOrDefault(c => c.OwnerClientId == message.OwnerClientId);

            if (character == null) return;

            var container = character.GetComponentInChildren<CharacterMessageContainerView>()?.transform;

            if (container == null) return;

            MessageViewBase prefab = GetPrefab(message.Type);
            if (prefab == null) return;

            var view = _viewFactory.Create(prefab, Vector3.zero, Quaternion.identity, container);

            if (view == null) return;

            view.SetMessage(message);
            view.SetStateVisible(true);

            _activeViews[message.InstanceId] = view;
        }

        private void OnDestroyMessage(DestroyChatMessageSignal signal)
        {
            var id = signal.Message.InstanceId;
            if (_activeViews.TryGetValue(id, out var view))
            {
                if (view != null && view is MonoBehaviour mb && mb != null)
                {
                    view.SetStateVisible(false);
                }
                _activeViews.Remove(id);
            }
        }

        private MessageViewBase GetPrefab(MessageType type) => type switch
        {
            MessageType.Text => _viewConfig.TextMessagePrefab,
            MessageType.Emoji => _viewConfig.EmotjiPrefab,
            _ => null
        };
    }
}