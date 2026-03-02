using AvatarChat.Network.Models;
using UnityEngine;

namespace AvatarChat.UI.Views
{
    public abstract class MessageViewBase : MonoBehaviour, IMessageView
    {
        private NetworkMessage _message;
        private Transform _startParent;

        private void Awake()
        {
            _startParent = transform.parent;
        }
        public void SetMessage(NetworkMessage message)
        {
            _message = message;
            OnMessageSet(message);
        }

        public void SetStateVisible(bool visible)
        {
            if (!visible)
            {
                transform.SetParent(_startParent, false);
            }
            gameObject.SetActive(visible);
        }

        protected abstract void OnMessageSet(NetworkMessage message);
    }
}
