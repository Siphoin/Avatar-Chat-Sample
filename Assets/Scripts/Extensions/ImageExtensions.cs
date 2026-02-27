using UnityEngine;
using UnityEngine.UI;

namespace AvatarChat.Core.Extensions
{
    public static class ImageExtensions
    {
        public static void SetAdaptiveSize(this Image image)
        {
            try
            {
                float aspectRatio = (float)image.sprite.texture.width / image.sprite.texture.height;
                float newWidth = image.rectTransform.sizeDelta.y * aspectRatio;
                image.rectTransform.sizeDelta = new Vector2(newWidth, image.rectTransform.sizeDelta.y);
            }
            catch
            {
            }
        }
    }
}
