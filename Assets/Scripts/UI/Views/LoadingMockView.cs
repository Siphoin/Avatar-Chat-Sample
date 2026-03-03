using UnityEngine;

namespace AvatarChat.UI.Views
{
    [RequireComponent(typeof(RectTransform))]
    public class LoadingMockView : MonoBehaviour, ILoadingMock
    {
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Show(Transform parent)
        {
            transform.SetParent(parent, false);

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}