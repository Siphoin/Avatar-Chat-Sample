using AvatarChat.Main;
using AvatarChat.Meta.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AvatarChat.Meta.SO
{
    [CreateAssetMenu(fileName = "ScriptableCharacter", menuName = "AvatarChat/Meta/ScriptableCharacter")]
    public class ScriptableCharacter : ScriptableObjectIdentity
    {
        [SerializeField] private CharacterDirectionData[] _directionData;
        public IEnumerable<CharacterDirectionData> DirectionData => _directionData;

        private readonly List<AsyncOperationHandle<Sprite>> _handles = new();

        private bool _isLoaded;
        private int _referenceCount;
        private Task _loadingTask;

        public async Task LoadSpritesAsync()
        {
            _referenceCount++;

            if (_isLoaded) return;

            if (_loadingTask != null && !_loadingTask.IsCompleted)
            {
                await _loadingTask;
                return;
            }

            _loadingTask = InternalLoadAsync();
            await _loadingTask;
        }

        private async Task InternalLoadAsync()
        {
            foreach (var data in _directionData)
            {
                var handle = data.SpriteReference.LoadAssetAsync();
                _handles.Add(handle);
            }

            foreach (var handle in _handles)
            {
                await handle.Task;
            }

            _isLoaded = true;
        }

        public void ReleaseSprites()
        {
            _referenceCount--;

            if (_referenceCount <= 0)
            {
                foreach (var handle in _handles)
                {
                    if (handle.IsValid()) Addressables.Release(handle);
                }
                _handles.Clear();
                _isLoaded = false;
                _loadingTask = null;
                _referenceCount = 0;
            }
        }

        public Sprite GetSpriteForDirection(CharacterDirectionType direction)
        {
            foreach (var data in _directionData)
            {
                if (data.Direction == direction)
                {
                    return data.SpriteReference.Asset as Sprite;
                }
            }
            return null;
        }
    }
}