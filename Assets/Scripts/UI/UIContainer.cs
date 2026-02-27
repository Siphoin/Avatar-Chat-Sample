using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AvatarChat.UI
{
    [RequireComponent(typeof(Canvas))]
    public class UIContainer : MonoBehaviour, IUIContainer
    {
        [SerializeField, ReadOnly] private Canvas _canvas;

        private void OnValidate()
        {
            if (!_canvas)
            {
                _canvas = GetComponent<Canvas>();
            }
        }

        public void AddElement (RectTransform element)
        {
            element.SetParent(transform, false);
        }
    }
}
