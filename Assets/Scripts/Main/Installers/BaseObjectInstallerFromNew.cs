using UnityEngine;

namespace AvatarChat.Main.Installers
{
    public class BaseObjectInstallerFromNew<T> : BaseObjectInstaller where T : MonoBehaviour
    {
        [SerializeField] private T _prefab;
        [SerializeField] private bool _dontDestroyOnLoad = true;
        public override void InstallBindings()
        {
            T newObject = Container.InstantiatePrefabForComponent<T>(_prefab);
            var bind = Container.Bind<T>().FromInstance(newObject);

            if (AsSingle)
            {
                bind.AsSingle();
            }

            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(newObject);
            }
        }
    }

    public class BaseObjectInstallerFromNew<TImplement, TInterface> : BaseObjectInstaller where TImplement : MonoBehaviour, TInterface
    {
        [SerializeField] private bool _dontDestroyOnLoad = true;
        [SerializeField] private TImplement _prefab;
        public override void InstallBindings()
        {
            TImplement newObject = Container.InstantiatePrefabForComponent<TImplement>(_prefab);
            var bind = Container.Bind<TInterface>().To<TImplement>().FromInstance(newObject);

            if (AsSingle)
            {
                bind.AsSingle();
            }

            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(newObject.gameObject);
            }
        }
    }
}
