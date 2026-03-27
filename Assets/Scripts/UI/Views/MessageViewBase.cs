using AvatarChat.Network.Models;
using AvatarChat.UI.Configs;
using AvatarChat.UI.Animations;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace AvatarChat.UI.Views
{
    public abstract class MessageViewBase : MonoBehaviour, IMessageView
    {
        [SerializeField] private Image _background;
        private MessageAppearanceAnimation _animation;

        [Inject] private MessageViewConfig _config;

        private Transform _startParent;

        private void Awake()
        {
            _animation = GetComponent<MessageAppearanceAnimation>();
            _startParent = transform.parent;
        }

        private void LateUpdate()
        {
            UpdateTransparency();
        }

        public void SetMessage(NetworkMessage message)
        {
            OnMessageSet(message);
        }

        public void SetStateVisible(bool visible)
        {
            if (this == null) return;

            if (visible)
            {
                if (gameObject != null)
                {
                    gameObject.SetActive(true);
                    if (_animation != null)
                    {
                        _animation.Play();
                    }
                }
            }
            else
            {
                if (_animation != null && gameObject != null && gameObject.activeInHierarchy)
                {
                    _animation.Hide(() =>
                    {
                        if (this != null && gameObject != null)
                        {
                            FinalizeHide();
                        }
                    });
                }
                else
                {
                    FinalizeHide();
                }
            }
        }

        private void FinalizeHide()
        {
            if (this == null || gameObject == null) return;

            if (_startParent != null)
            {
                transform.SetParent(_startParent, false);
            }

            gameObject.SetActive(false);
        }

        private void UpdateTransparency()
        {
            if (this == null || _background == null || transform == null || transform.parent == null) return;

            bool isLastChild = transform.GetSiblingIndex() == transform.parent.childCount - 1;

            Color color = _background.color;
            float targetAlpha = isLastChild ? 1f : _config.PercentTransperentOldMessage / 100f;

            if (!Mathf.Approximately(color.a, targetAlpha))
            {
                color.a = targetAlpha;
                _background.color = color;
            }
        }

        protected abstract void OnMessageSet(NetworkMessage message);
    }
}