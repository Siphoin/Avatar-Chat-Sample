using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace AvatarChat.Main.Services
{
    [CreateAssetMenu(menuName = "System/Services/Startup")]
    public class Startup : ScriptableService, IInitializable
    {
        [SerializeField, MinValue(30)] private int _targetFPS = 60;
        [SerializeField] private bool _isDevBuild;
#if UNITY_EDITOR
        [SerializeField] private bool _loadStartupScene;
#endif

        public bool IsDevBuild => _isDevBuild;

        public void Initialize()
        {
#if UNITY_EDITOR
            if (_loadStartupScene)
            {
                SceneManager.LoadScene(0);
            }
#endif
            Application.targetFrameRate = _targetFPS;
            Application.runInBackground = true;
        }
    }
}
