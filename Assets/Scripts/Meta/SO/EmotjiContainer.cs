using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using AvatarChat.Main.Configs;

namespace Assets.Scripts.Meta.SO
{
    [CreateAssetMenu(fileName = "EmotjiContainer", menuName = "Meta/Emotji Container")]
    public class EmotjiContainer : ScriptableConfig
    {
        [SerializeField] private AssetReferenceSprite[] _emotjis;

        public async UniTask<Sprite> LoadEmotji(int index)
        {
            if (index < 0 || index >= _emotjis.Length) return null;

            Sprite sprite = await Addressables.LoadAssetAsync<Sprite>(_emotjis[index].RuntimeKey);
            return sprite;
        }

        public void ReleaseEmotji(Sprite sprite)
        {
            if (sprite != null)
            {
                Addressables.Release(sprite);
            }
        }
    }
}