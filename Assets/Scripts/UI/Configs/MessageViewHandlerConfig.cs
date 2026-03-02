using AvatarChat.Main.Configs;
using AvatarChat.UI.Views;
using UnityEngine;

namespace AvatarChat.UI.Configs
{
    [CreateAssetMenu(fileName = "MessageViewHandlerConfig", menuName = "Configs/UI/MessageViewHandler")]
    public class MessageViewHandlerConfig : ScriptableConfig
    {
        [SerializeField] private EmotjiMessageView _emotjiPrefab;
        [SerializeField] private TextMessageView _textMessagePrefab;

        public EmotjiMessageView EmotjiPrefab => _emotjiPrefab;
        public TextMessageView TextMessagePrefab => _textMessagePrefab;
    }
}
