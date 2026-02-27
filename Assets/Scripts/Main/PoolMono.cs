using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Zenject;
using System.Collections;

namespace AvatarChat.Main
{
    /// <summary>
    /// Служит для пулла объектов MonoBehaviour
    /// </summary>
    /// <typeparam name="T">Тип префаба, который будет использоваться</typeparam>
    /// 

    public class PoolMono<T> : IEnumerable<T> where T : MonoBehaviour 
    {
        /// <summary>
        /// Коллекция пулла, в котором находятся объекты для вытаскивания, может расширяться, если включен флаг AutoExpand
        /// </summary>
        private List<T> _pool;


        /// <summary>
        /// Какой префаб создавать или включать/отключать
        /// </summary>
        public T Prefab { get; private set; }


        /// <summary>
        /// В него складываются все объекты пулла как дочерние
        /// </summary>

        public Transform Container { get; private set; }

        /// <summary>
        /// Может ли пулл расширятся, если не хватает объектов
        /// </summary>
        public bool AutoExpand { get; private set; }

        public bool ActiveAfterGetting { get; private set; } = true;

        public Vector3 StartPosition { get; private set; }

        public DiContainer DiContainer { get;  set; }

        public int CountActiveElements
        {
            get
            {
                int count = 0;
                foreach (var mono in _pool)
                {
                    if (mono.gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        

        public PoolMono(T prefab, Transform container, DiContainer diContainer, int count, bool autoExpand = false, bool activateAfterGetting = true)
        {
            if (prefab is null)
            {
                throw new NullReferenceException("prefab on pool not be null");
            }

            if (count <= 0)
            {
                throw new NullReferenceException("count of pool object not be lesser and equals 0");
            }


            Prefab = prefab;

            Container = container;

            AutoExpand = autoExpand;

            ActiveAfterGetting = activateAfterGetting;

           DiContainer = diContainer;

            CreatePool(count);

        }

        public PoolMono(T prefab, int count, Vector3 startPosition, bool autoExpand = false, bool activateAfterGetting = true)
        {
            if (prefab is null)
            {
                throw new NullReferenceException("prefab on pool not be null");
            }

            if (count <= 0)
            {
                throw new NullReferenceException("count of pool object not be lesser and equals 0");
            }


            Prefab = prefab;
            
            AutoExpand = autoExpand;

            ActiveAfterGetting = activateAfterGetting;

            StartPosition = startPosition;

            CreatePool(count);
        }

        public PoolMono(T prefab, IEnumerable<T> objects, bool autoExpand = false)
        {

            if (prefab is null)
            {
                throw new NullReferenceException("prefab on pool not be null");
            }

            Prefab = prefab;
            
            AutoExpand = autoExpand;
            
            CreatePool(objects);
        }

        public PoolMono(T prefab, IEnumerable<T> objects, Transform container, bool autoExpand = false, bool activateAfterGetting = true)
        {

            if (prefab is null)
            {
                throw new NullReferenceException("prefab on pool not be null");
            }

            Prefab = prefab;
            
            AutoExpand = autoExpand;

            ActiveAfterGetting = activateAfterGetting;

            Container = container;

            CreatePool(objects);
        }

        private void CreatePool(int count)
        {
            _pool = new List<T>();

            for (int i = 0; i < count; i++)
            {
                CreateObject();
            }
        }

        private void CreatePool(IEnumerable<T> objects)
        {

            _pool = new List<T>();

            var array = objects.ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                MonoBehaviour monoBehaviour = array[i];

                GameObject gameObject = monoBehaviour.gameObject;

                if (Container != null) 
                {
                    gameObject.transform.SetParent(Container, false);
                }

                gameObject.SetActive(false);
                
                _pool.Add(array[i]);
            }
        }

        private T CreateObject (bool isActiveByDefault = false)
        {
        
            T createdObject = DiContainer is null ? UnityEngine.Object.Instantiate(Prefab, Container) : DiContainer.InstantiatePrefabForComponent<T>(Prefab, Container);

            createdObject.gameObject.SetActive(isActiveByDefault);

            _pool.Add(createdObject);

            createdObject.gameObject.name = $"{Prefab.name}{_pool.Count}";

            return createdObject;
        }

        private bool HasFreeElement (out T element)
        {
            ValidatePool();
            foreach (var mono in _pool)
            {
                if (!mono.gameObject.activeInHierarchy)
                {
                    element = mono;
                    element.gameObject.SetActive(true);
                    return true;
                }
            }

            element = null;

            return false;
        }

        public bool HasFreeElement()
        {
            ValidatePool();
            foreach (var mono in _pool)
            {
                if (!mono.gameObject.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        private void ValidatePool()
        {
            _pool = _pool.Where(x => x != null && x.gameObject != null).ToList();
        }

        public T GetFreeElement ()
        {
            if (HasFreeElement(out T element))
            {
                return element;
            }

            if (AutoExpand)
            {
                return CreateObject(ActiveAfterGetting);
            }

            throw new ArgumentOutOfRangeException($"there is no free elements in pool is type {typeof(T)}");
        }

        public T GetElementByIndex (int index)
        {
            if (index < 0 || index >= _pool.Count)
            {
                throw new ArgumentOutOfRangeException($"index argument of element in pool");
            }

            T element = _pool[index];

            return element;
        }

        public bool Contains (T element)
        {
            return _pool.Contains(element);
        }

       public void ReturnToPool (T element)
       {
            if (!Contains(element))
            {
                throw new ArgumentException($"element {element.name} not contains on pool");
            }

            element.gameObject.SetActive(false);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _pool.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
