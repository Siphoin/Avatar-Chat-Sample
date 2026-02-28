using AvatarChat.Core.Components;
using AvatarChat.Main.Configs;
using UnityEngine;

namespace AvatarChat.Core.Configs
{
    [CreateAssetMenu(fileName = "PlayerSpawnerHandlerConfig", menuName = "Configs/Network/Player Spawner Config")]
    public class PlayerSpawnerHandlerConfig : ScriptableConfig
    {
        [SerializeField] private Character _playerPrefab;

        public Character PlayerPrefab => _playerPrefab;
    }
}