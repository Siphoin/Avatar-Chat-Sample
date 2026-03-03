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
        [Inject] private ILoadingMockHandler _loadingMockHandler;

        protected override void OnMessageSet(NetworkMessage message)
        {
            LoadAndShowEmoji(message).Forget();
        }

        private async UniTaskVoid LoadAndShowEmoji(NetworkMessage message)
        {
            var mockLoading = _loadingMockHandler.Show(_emojiContent.transform);
            _emojiContent.sprite = null;

            var sprite = await _emojiProvider.GetSpriteForMessage(message);

            if (sprite != null)
            {
                _emojiContent.sprite = sprite;
                _emojiContent.SetAdaptiveSize();
            }

            _loadingMockHandler.Hide(mockLoading);
        }
    }
}