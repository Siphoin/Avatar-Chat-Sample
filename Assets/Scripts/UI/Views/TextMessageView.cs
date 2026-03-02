using AvatarChat.Network.Models;
using System.Collections;
using TMPro;
using UnityEngine;

namespace AvatarChat.UI.Views
{
    public class TextMessageView : MessageViewBase
    {
        [SerializeField] private TextMeshProUGUI _textContent;

        protected override void OnMessageSet(NetworkMessage message)
        {
            _textContent.text = System.Text.Encoding.UTF8.GetString(message.Data);
        }
    }
}