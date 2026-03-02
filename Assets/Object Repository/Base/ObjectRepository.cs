using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ObjectRepositories
{
    internal abstract class ObjectRepository
    {
        private static Dictionary<Type, ObjectRepository> _repositories = new();

        internal static IObjectRepository<T> GetInstance<T>()
        {
            Type type = typeof(T);
            if (!_repositories.TryGetValue(type, out var repository))
            {
                repository = new ObjectRepository<T>();
                _repositories.Add(type, repository);
                return repository as IObjectRepository<T>;
            }
            else
            {
                return repository as IObjectRepository<T>;
            }


        }
    }
    internal sealed class ObjectRepository<T> : ObjectRepository, IEnumerable<T>, IObjectRepository<T>
    {
        private readonly List<T> _objects = new List<T>();

        public IEnumerator<T> GetEnumerator()
        {
            return _objects.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void AddObject(T obj)
        {
            _objects.Add(obj);
            
        }

        public void RemoveObject(T obj)
        {
            _objects.Remove(obj);
        }
    }

    internal interface IObjectRepository<T> : IEnumerable<T>
    {
        void AddObject(T obj);
        void RemoveObject(T obj);
    }
}
