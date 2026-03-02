using AvatarChat.Main.Configs;
using UnityEngine;

namespace AvatarChat.UI.Animations
{
    [CreateAssetMenu(fileName = "MessageAnimationConfig", menuName = "Configs/Animations/MessageAnimation")]
    public class MessageAnimationConfig : ScriptableConfig
    {
        [SerializeField] private float _duration = 0.3f;
        [SerializeField] private float _startScale = 0.8f;
        [SerializeField] private float _yOffset = -20f;

        public float Duration => _duration;
        public float StartScale => _startScale;
        public float YOffset => _yOffset;
    }
}