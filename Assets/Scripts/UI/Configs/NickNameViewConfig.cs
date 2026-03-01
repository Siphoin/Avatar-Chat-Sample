using AvatarChat.Main.Configs;
using UnityEngine;

namespace AvatarChat.UI.Configs
{
    [CreateAssetMenu(fileName = "NickNameViewConfig", menuName = "Configs/UI/NickName")]
    public class NickNameViewConfig : ScriptableConfig
    {
        [SerializeField] private Color _ownerNameColor = Color.red;
        [SerializeField] private Color _friendNameColor = Color.green;
        [SerializeField] private Color _noFriendColor = Color.white;

        public Color OwnerNameColor => _ownerNameColor;
        public Color FriendNameColor => _friendNameColor;
        public Color NoFriendColor => _noFriendColor;
    }
}
