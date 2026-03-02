using AvatarChat.Network.Models;
using UnityEngine;
using UnityEngine.UI;
namespace AvatarChat.UI.Views
{
    public class EmotjiMessageView : MessageViewBase
    {
        [SerializeField] private Image _emojiContent;

        protected override void OnMessageSet(NetworkMessage message)
        {
        }
    }
}