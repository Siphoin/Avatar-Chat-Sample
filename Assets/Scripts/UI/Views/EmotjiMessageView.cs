using AvatarChat.Core.Extensions;
using AvatarChat.Network.Models;
using AvatarChat.UI.Handlers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace AvatarChat.UI.Views
{
    public class EmotjiMessageView : MessageViewBase
    {
        [SerializeField] private Image _emojiContent;
        [Inject] private IEmotjiProviderHandler _emojiProvider;

        protected override void OnMessageSet(NetworkMessage message)
        {
            LoadAndShowEmoji(message).Forget();
        }

        private async UniTaskVoid LoadAndShowEmoji(NetworkMessage message)
        {
            _emojiContent.sprite = null;

            var sprite = await _emojiProvider.GetSpriteForMessage(message);

            if (sprite != null)
            {
                _emojiContent.sprite = sprite;
                _emojiContent.SetAdaptiveSize();
            }
        }
    }
}