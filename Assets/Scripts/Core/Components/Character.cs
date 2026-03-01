using AvatarChat.Network.Components;
using UnityEngine;
namespace AvatarChat.Core.Components
{
    [RequireComponent(typeof(CharacterMovement))]
    public class Character : NetworkObject
    {
        protected override void OnPlayerReady()
        {
            Debug.Log(Owner.Name + " is ready!");
        }
    }
}
