using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AvatarChat.Meta.Models
{
    [Serializable]
    public struct CharacterDirectionData
    {
        [SerializeField] private CharacterDirectionType _direction;
        [SerializeField] private AssetReferenceSprite _spriteReference;

        public AssetReferenceSprite SpriteReference => _spriteReference;

        public CharacterDirectionType Direction => _direction;
    }
}