using AvatarChat.Main.Configs;
using Sirenix.OdinInspector;
using UnityEngine;
namespace AvatarChat.UI.Configs
{
    public abstract class FactoryConfig : ScriptableConfig
    {
        [SerializeField, MinValue(1)] private int _startCount = 1;
        public int StartCount => _startCount;
    }
    public abstract class FactoryConfig<TPrefab> : FactoryConfig where TPrefab : UnityEngine.Object
    {
        [SerializeField] private TPrefab _prefab;
        public TPrefab Prefab => _prefab;
    }


}
