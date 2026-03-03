using AvatarChat.Main;
using AvatarChat.UI.Configs;
using AvatarChat.UI.Views;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace AvatarChat.UI
{
    public class LoadingMockHandler : MonoBehaviour, ILoadingMockHandler
    {
        [Inject] private LoadingMockHandlerConfig _config;
        [SerializeField] private DiContainer _diContainer;
        private PoolMono<LoadingMockView> _pool;
        private GameObject _loadedPrefab;

        private void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTask InitializeAsync()
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(_config.Address);
            _loadedPrefab = await handle.ToUniTask();

            if (_loadedPrefab != null)
            {
                var view = _loadedPrefab.GetComponent<LoadingMockView>();
                _pool = new PoolMono<LoadingMockView>(view, transform, _diContainer, _config.InitialPoolSize, true);
            }
        }

        public ILoadingMock Show(Transform parent)
        {
            if (_pool == null) return null;

            var view = _pool.GetFreeElement();
            view.Show(parent);
            return view;
        }

        public void Hide(ILoadingMock view)
        {
            if (view is LoadingMockView concreteView && _pool != null)
            {
                _pool.ReturnToPool(concreteView);
            }
        }

        private void OnDestroy()
        {
            if (_loadedPrefab != null)
            {
                Addressables.Release(_loadedPrefab);
            }
        }
    }
}