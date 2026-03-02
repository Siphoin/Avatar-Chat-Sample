using AvatarChat.Main.Factories;
using AvatarChat.UI.Views;
using System;
using UnityEngine;
using UniRx;
using AvatarChat.Main;
using Zenject;
using AvatarChat.UI.Configs;
using System.Collections.Generic;

namespace AvatarChat.UI.Factories
{
    public class MessageViewFactory : IMessageViewFactory
    {
        private Subject<IMessageView> _onSpawn = new();
        private readonly Dictionary<Type, PoolMono<MessageViewBase>> _pools = new();

        [Inject] private MessageViewFactoryConfig _config;
        [Inject] private DiContainer _container;

        public IObservable<IMessageView> OnSpawn => _onSpawn;

        public IMessageView Create(MessageViewBase prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Type type = prefab.GetType();

            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new PoolMono<MessageViewBase>(prefab, null, _container, _config.StartCount, true);
                _pools.Add(type, pool);
            }

            var view = pool.GetFreeElement();
            view.transform.SetParent(parent, false);
            view.transform.SetPositionAndRotation(position, rotation);

            _onSpawn.OnNext(view);
            return view;
        }
    }
}