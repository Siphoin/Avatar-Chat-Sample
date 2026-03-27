using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Zenject;
using System;

namespace AvatarChat.UI.Animations
{
    public class MessageAppearanceAnimation : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private RectTransform _rectTransform;

        [Inject] private MessageAnimationConfig _animConfig;

        private Sequence _activeSequence;
        private bool _isInitialized;

        private void Awake()
        {
            _isInitialized = true;
        }

        public void Play()
        {
            _activeSequence?.Kill();

            Color color = _background.color;
            color.a = 0;
            _background.color = color;

            _rectTransform.localScale = Vector3.one * _animConfig.StartScale;

            _activeSequence = DOTween.Sequence();
            _activeSequence.Join(_background.DOFade(1f, _animConfig.Duration));
            _activeSequence.Join(_rectTransform.DOScale(1f, _animConfig.Duration)
                .SetEase(Ease.OutBack));
        }

        public void Hide(Action onComplete)
        {
            _activeSequence?.Kill();

            _activeSequence = DOTween.Sequence();
            _activeSequence.Join(_background.DOFade(0f, _animConfig.Duration));
            _activeSequence.Join(_rectTransform.DOScale(_animConfig.StartScale, _animConfig.Duration)
                .SetEase(Ease.InBack));

            _activeSequence.OnComplete(() =>
            {
                if (gameObject != null)
                {
                    onComplete?.Invoke();
                }
            });
        }

        private void OnDestroy()
        {
            _activeSequence?.Kill();
        }
    }
}