using UnityEngine;

namespace AvatarChat.UI
{
    public interface ILoadingMockHandler
    {
        void Hide(ILoadingMock view);
        ILoadingMock Show(Transform parent);
    }
}