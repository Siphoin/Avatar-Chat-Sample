using AvatarChat.Main.Configs;
using Sirenix.OdinInspector;
using UnityEngine;
namespace AvatarChat.UI.Configs
{
    public abstract class FactoryConfig<TPrefab> : ScriptableConfig where TPrefab : UnityEngine.Object
    {
        [SerializeField] private TPrefab _prefab;
        [SerializeField, MinValue(1)] private int _startCount = 1;
        public TPrefab Prefab => _prefab;

        public int StartCount => _startCount;
    }
}
