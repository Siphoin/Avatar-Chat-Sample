using AvatarChat.Main.Configs;
using UnityEngine;

namespace AvatarChat.UI.Configs
{
    [CreateAssetMenu(fileName = "MessageViewConfig", menuName = "Configs/MessageViewConfig")]
    public class MessageViewConfig : ScriptableConfig
    {
        [SerializeField, Range(0, 100)] private float _percentTransperentOldMessage = 25;

        public float PercentTransperentOldMessage => _percentTransperentOldMessage;
    }
}
