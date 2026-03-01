using AvatarChat.Core.Components;
using AvatarChat.UI.Configs;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Zenject;

namespace AvatarChat.UI.Views
{
    public class NickNameView : MonoBehaviour
    {
        [SerializeField, ReadOnly] private TextMeshProUGUI _textComponent;
        [SerializeField] private Character _character;
        [Inject] private NickNameViewConfig _config;

        private async void Start()
        {
            _textComponent.text = string.Empty;
            var token = this.GetCancellationTokenOnDestroy();
            await UniTask.WaitUntil(() => !_character.Owner.IsEmpty, cancellationToken: token);
            _textComponent.text = _character.Owner.Name.ToString();
            _textComponent.color = GetColor();

        }

        private Color GetColor ()
        {
            return _character.IsOwner ? _config.OwnerNameColor : _config.NoFriendColor;
        }

        private void OnValidate()
        {
            if (!_textComponent)
            {
                _textComponent = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}