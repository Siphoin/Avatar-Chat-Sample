using AvatarChat.Main.Configs;
using Unity.Netcode;
using UnityEngine;

namespace AvatarChat.Network.Configs
{
    [CreateAssetMenu(fileName = "NetworkHandlerConfig", menuName = "Configs/Network/Handler")]
    public class NetworkHandlerConfig : ScriptableConfig
    {
        [SerializeField] private NetworkManager _networkManagerPrefab;
        public NetworkManager NetworkManagerPrefab => _networkManagerPrefab;
    }
}