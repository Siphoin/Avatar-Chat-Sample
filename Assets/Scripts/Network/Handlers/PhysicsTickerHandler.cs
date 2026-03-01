using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AvatarChat.Network.Handlers
{
    public class PhysicsTickerHandler : MonoBehaviour
    {
        private Scene _scene;
        private PhysicsScene2D _physicsScene;

        private void Awake()
        {
            _scene = gameObject.scene;
            _physicsScene = _scene.GetPhysicsScene2D();
        }

        private void FixedUpdate()
        {
            if (_physicsScene.IsValid() && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                _physicsScene.Simulate(Time.fixedDeltaTime);
        }
    }
}
