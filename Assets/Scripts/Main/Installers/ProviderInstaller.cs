using UnityEngine;
using Zenject;
namespace AvatarChat.Main.Installers
{
    public abstract class ProviderInstaller<T> : MonoInstaller where T : UnityEngine.Object
    {
        [SerializeField] private T[] _elements = new T[0];

        public override void InstallBindings()
        {
            for (int i = 0; i < _elements.Length; i++)
            {
                var element = _elements[i];;
                Container.Bind(element.GetType()).FromInstance(element);

                if (element is IInitializable initializable)
                {
                    initializable.Initialize();
                }
            }
        }
    }

}