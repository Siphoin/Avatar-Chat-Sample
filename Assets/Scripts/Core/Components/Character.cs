using AvatarChat.Meta.SO;
using AvatarChat.Network.Components;
using UnityEngine;
namespace AvatarChat.Core.Components
{
    [RequireComponent(typeof(CharacterRepositoryRegister))]
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(CharacterRotationView))]
    public class Character : NetworkObject
    {
        [SerializeField] private ScriptableCharacter _characterData;

        public ScriptableCharacter CharacterData => _characterData;

        protected override void OnPlayerReady()
        {
            Debug.Log(Owner.Name + " is ready!");
        }
    }
}
