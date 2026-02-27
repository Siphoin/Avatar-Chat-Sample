using System;
using UnityEngine;
namespace AvatarChat.Main
{
    [Serializable]
    public class KeyValueElement<TKey, TValue>
    {
        [SerializeField] private TKey _key;
        [SerializeField] private TValue _value;

        public KeyValueElement()
        {

        }

        public KeyValueElement(TKey key, TValue value)
        {
            _key = key;
            _value = value;
        }

        public TKey Key => _key;

        public TValue Value => _value;
    }

}