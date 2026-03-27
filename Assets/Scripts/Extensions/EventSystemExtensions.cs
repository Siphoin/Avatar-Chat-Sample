using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AvatarChat.Extensions
{
    public static class EventSystemExtensions
    {
        public static bool IsPointerOverUIObject(this EventSystem eventSystem)
        {
            Vector2 position = Vector2.zero;

#if UNITY_STANDALONE
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif
#if UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0)
            {
                var positionTouch = Input.GetTouch(0).position;
                position = new Vector2(positionTouch.x, positionTouch.y);
            }
#endif

            var eventDataCurrentPosition = new PointerEventData(eventSystem)
            {
                position = new Vector2(position.x, position.y)
            };

            List<RaycastResult> results = new List<RaycastResult>();
            eventSystem.RaycastAll(eventDataCurrentPosition, results);

            return results.Any(result => result.gameObject.layer == LayerMask.NameToLayer("UI"));
        }

        public static bool TryGetComponentFromSelectedRectTransform<T>(this EventSystem eventSystem, out T component)
        {
            return TryGetComponentFromRectTransform(eventSystem, eventSystem.currentSelectedGameObject, out component);
        }

        public static bool TryGetComponentFromFirstSelectedRectTransform<T>(this EventSystem eventSystem, out T component)
        {
            return TryGetComponentFromRectTransform(eventSystem, eventSystem.firstSelectedGameObject, out component);
        }

        private static bool TryGetComponentFromRectTransform<T>(EventSystem eventSystem, GameObject gameObject, out T component)
        {
            component = default;

            if (!gameObject)
            {
                return false;
            }
            if (!gameObject.TryGetComponent(out RectTransform _))
            {
                return false;
            }

            gameObject.TryGetComponent(out component);
            return component != null;
        }
    }
}
