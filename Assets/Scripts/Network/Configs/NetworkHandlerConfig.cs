using AvatarChat.Main.Configs;
using Unity.Netcode;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

namespace AvatarChat.Network.Configs
{
    [CreateAssetMenu(fileName = "NetworkHandlerConfig", menuName = "Configs/Network/Handler")]
    public class NetworkHandlerConfig : ScriptableConfig
    {
        [TabGroup("General")]
        [SerializeField] private NetworkManager _networkManagerPrefab;
        public NetworkManager NetworkManagerPrefab => _networkManagerPrefab;

        [TabGroup("Transport")]
        [SerializeField] private string _address = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;

        public string Address => _address;
        public ushort Port => _port;

        [TabGroup("Keys")]
        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Action", ValueLabel = "Key")]
        private Dictionary<string, KeyCode> _hotkeys = new Dictionary<string, KeyCode>()
        {
            { "Host", KeyCode.H },
            { "Client", KeyCode.C },
            { "Server", KeyCode.S },
            { "Shutdown", KeyCode.Q }
        };

        public KeyCode GetHotkey(string action)
        {
            if (_hotkeys.TryGetValue(action, out var key))
            {
                return key;
            }
            return KeyCode.None;
        }
    }
}