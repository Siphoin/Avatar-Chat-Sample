using UnityEngine;

namespace AvatarChat.UI
{
    public interface ILoadingMock
    {
        void Show(Transform parent);
        void Hide();
    }
}