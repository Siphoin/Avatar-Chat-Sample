using AvatarChat.Meta.Models;
using AvatarChat.Meta.SO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using UnityEngine.EventSystems;
using AvatarChat.Extensions;

namespace AvatarChat.Core.Components
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterRotationView : AvatarChat.Network.Components.NetworkObject
    {
        private class SpriteCache
        {
            public int ReferenceCount;
            public AsyncLazy Loader;
            public List<AsyncOperationHandle<Sprite>> Handles = new();
        }

        private static readonly Dictionary<ScriptableCharacter, SpriteCache> _sharedCache = new();

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Character _character;

        private readonly NetworkVariable<CharacterDirectionType> _currentDirection = new(
            CharacterDirectionType.Down,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private CharacterDirectionType _lastClientSideDirection;
        private CancellationTokenSource _destroyCts;

        protected override async void OnPlayerReady()
        {
            _destroyCts = new CancellationTokenSource();

            if (_character != null && _character.CharacterData != null)
            {
                await EnsureSpritesLoadedAsync(_character.CharacterData).AttachExternalCancellation(_destroyCts.Token);
                UpdateSprite(_currentDirection.Value);
            }

            _currentDirection.OnValueChanged += OnDirectionChanged;

            if (IsOwner)
            {
                _lastClientSideDirection = _currentDirection.Value;
            }
        }

        private async UniTask EnsureSpritesLoadedAsync(ScriptableCharacter data)
        {
            if (!_sharedCache.TryGetValue(data, out var cache))
            {
                cache = new SpriteCache();
                cache.Loader = UniTask.Lazy(() => LoadInternalAsync(data, cache));
                _sharedCache.Add(data, cache);
            }

            cache.ReferenceCount++;

            await cache.Loader.Task;
        }

        private async UniTask LoadInternalAsync(ScriptableCharacter data, SpriteCache cache)
        {
            var directions = data.DirectionData;
            var loadTasks = new List<UniTask>();

            foreach (var direction in directions)
            {
                var handle = direction.SpriteReference.LoadAssetAsync<Sprite>();
                cache.Handles.Add(handle);
                loadTasks.Add(handle.ToUniTask());
            }

            await UniTask.WhenAll(loadTasks);
        }

        public override void OnNetworkDespawn()
        {
            _currentDirection.OnValueChanged -= OnDirectionChanged;

            if (_destroyCts != null)
            {
                _destroyCts.Cancel();
                _destroyCts.Dispose();
            }

            if (_character != null && _character.CharacterData != null)
            {
                ReleaseSprites(_character.CharacterData);
            }
        }

        private void ReleaseSprites(ScriptableCharacter data)
        {
            if (_sharedCache.TryGetValue(data, out var cache))
            {
                cache.ReferenceCount--;
                if (cache.ReferenceCount <= 0)
                {
                    foreach (var handle in cache.Handles)
                    {
                        if (handle.IsValid()) Addressables.Release(handle);
                    }
                    _sharedCache.Remove(data);
                }
            }
        }

        private void Update()
        {
            if (IsOwner && Application.isFocused && !EventSystem.current.IsPointerOverUIObject())
            {
                HandleLookInput();
            }
        }

        private void HandleLookInput()
        {
            if (Camera.main == null) return;

            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (IsPointerOverCharacter(mouseWorldPos))
                return;

            Vector2 directionVector = (mouseWorldPos - (Vector2)transform.position).normalized;

            CharacterDirectionType newDirection = CalculateDirection(directionVector);

            if (newDirection != _lastClientSideDirection)
            {
                _lastClientSideDirection = newDirection;
                RequestChangeDirectionServerRpc(newDirection);
                UpdateSprite(newDirection);
            }
        }

        private bool IsPointerOverCharacter(Vector2 mouseWorldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
            return hit != null && hit.transform == transform;
        }
        private CharacterDirectionType CalculateDirection(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360;

            return angle switch
            {
                >= 67.5f and < 112.5f => CharacterDirectionType.Up,
                >= 22.5f and < 67.5f => CharacterDirectionType.UpRight,
                >= 337.5f or < 22.5f => CharacterDirectionType.Right,
                >= 292.5f and < 337.5f => CharacterDirectionType.DownRight,
                >= 247.5f and < 292.5f => CharacterDirectionType.Down,
                >= 202.5f and < 247.5f => CharacterDirectionType.DownLeft,
                >= 157.5f and < 202.5f => CharacterDirectionType.Left,
                >= 112.5f and < 157.5f => CharacterDirectionType.UpLeft,
                _ => _lastClientSideDirection
            };
        }

        [ServerRpc]
        private void RequestChangeDirectionServerRpc(CharacterDirectionType direction)
        {
            _currentDirection.Value = direction;
        }

        private void OnDirectionChanged(CharacterDirectionType previous, CharacterDirectionType current)
        {
            if (IsOwner) return;
            UpdateSprite(current);
        }

        private void UpdateSprite(CharacterDirectionType direction)
        {
            if (_character != null && _character.CharacterData != null)
            {
                Sprite nextSprite = _character.CharacterData.GetSpriteForDirection(direction);
                if (nextSprite != null)
                {
                    _spriteRenderer.sprite = nextSprite;
                }
            }
        }

        private void OnValidate()
        {
            if (!_spriteRenderer) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (!_character) _character = GetComponent<Character>();
        }
    }
}