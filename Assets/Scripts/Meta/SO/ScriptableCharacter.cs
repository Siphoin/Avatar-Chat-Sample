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