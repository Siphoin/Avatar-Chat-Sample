using AvatarChat.Main.Configs;
using UnityEngine;
namespace AvatarChat.UI.Configs
{
    [CreateAssetMenu(fileName = "LoadingMockHandlerConfig", menuName = "AvatarChat/UI/Configs/LoadingMockHandlerConfig")]
    public class LoadingMockHandlerConfig : ScriptableConfig
    {
        [SerializeField] private string _address;
        [SerializeField] private int _initialPoolSize = 10;

        public string Address => _address;
        public int InitialPoolSize => _initialPoolSize;
    }
}
