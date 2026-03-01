using Sirenix.OdinInspector;
using UnityEngine;

namespace AvatarChat.Core.Components
{
    [RequireComponent(typeof(Camera))]  
    public class GameCamera : MonoBehaviour
    {
        [SerializeField, ReadOnly] private Camera _camera;
        private void Awake()
        {
            if (_camera == Camera.main)
            {
                return;
            }

            Destroy(gameObject);
        }


        private void OnValidate()
        {
            if (!_camera)
            {
                _camera = GetComponent<Camera>();
            }
        }
    }
}