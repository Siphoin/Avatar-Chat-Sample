using AvatarChat.Network.Models;
using UnityEngine;

namespace AvatarChat.UI.Views
{
    public abstract class MessageViewBase : MonoBehaviour, IMessageView
    {
        private NetworkMessage _message;

        public void SetMessage(NetworkMessage message)
        {
            _message = message;
            OnMessageSet(message);
        }

        public void SetStateVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        protected abstract void OnMessageSet(NetworkMessage message);
    }
}
