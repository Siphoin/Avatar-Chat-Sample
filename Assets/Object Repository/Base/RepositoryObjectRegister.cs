using System;
using UnityEngine;

namespace ObjectRepositories
{
    public class RepositoryObjectRegister<T> : MonoBehaviour where T : Component
    {
        private T _component;

        private IObjectRepository<T> Repository => ObjectRepository.GetInstance<T>();

        private T Component
        {
            get
            {
                if (_component is null)
                {
                    _component = GetComponent<T>();
                }
            
                return _component;
            }

        }
        protected virtual void OnEnable()
        {
            Repository.AddObject(Component);
        }

        protected virtual void OnDisable()
        {
            Repository.RemoveObject(Component);
        }

        
    }
}
