using AvatarChat.Main.Factories;
using AvatarChat.UI.Views;
using System;
using UnityEngine;
using UniRx;
using AvatarChat.Main;
using Zenject;
using AvatarChat.UI.Configs;
namespace AvatarChat.UI.Factories
{
    public class MessageViewFactory : IMessageViewFactory
    {
        private Subject<IMessageView> _onSpawn = new();
        private PoolMono<MessageViewBase> _pool;
        [Inject] private MessageViewFactoryConfig _config;
        [Inject] private DiContainer _container;
        public IObservable<IMessageView> OnSpawn => _onSpawn;


        public IMessageView Create(MessageViewBase prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_pool is null)
            {
                _pool = new(_config.Prefab, null, _container, _config.StartCount, true);
            }

            var view = _pool.GetFreeElement();
            view.transform.SetParent(parent, false);
            return view;
        }
    }
}
