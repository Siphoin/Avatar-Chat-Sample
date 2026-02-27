using System.Collections;
using UnityEngine;

namespace AvatarChat.Main.Installers
{
    public abstract class BaseObjectInstallerFromInstance<T> : BaseObjectInstaller
    {
        [SerializeField] private T _instance;
        public override void InstallBindings()
        {
            var bind = Container.Bind<T>().FromInstance(_instance);

            if (AsSingle)
            {
                bind.AsSingle();
            }
        }
    }

    public abstract class BaseObjectInstallerFromInstance<TImplement, TInterface> : BaseObjectInstaller
        where TImplement : TInterface
    {
        [SerializeField] private TImplement _instance;
        public override void InstallBindings()
        {
            var bind = Container.Bind<TInterface>().To<TImplement>().FromInstance(_instance);

            if (AsSingle)
            {
                bind.AsSingle();
            }
        }
    }
}