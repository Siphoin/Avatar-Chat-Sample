using AvatarChat.Main.Configs;
using UnityEngine;

namespace AvatarChat.Network.Configs
{
    [CreateAssetMenu(fileName = "NetworkChatHandlerConfig", menuName = "Configs/Network/NetworkChatHandler")]
    public class NetworkChatHandlerConfig : ScriptableConfig
    {
        [SerializeField] private int _maxMessageByPlayer = 3;
        [SerializeField] private float _lifeStyleMessage = 10;
        [SerializeField] private float _spamThresholdSeconds = 0.05f;

        public int MaxMessageByPlayer => _maxMessageByPlayer;
        public float LifeStyleMessage => _lifeStyleMessage;
        public float SpamThresholdSeconds => _spamThresholdSeconds;
    }
}
