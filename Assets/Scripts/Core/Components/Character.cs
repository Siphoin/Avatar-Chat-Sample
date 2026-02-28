using AvatarChat.Network.Components;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine;
namespace AvatarChat.Core.Components
{
    public class Character : NetworkObject
    {
        protected override void OnPlayerReady()
        {
            Debug.Log(Owner.Name + " is ready!");
        }
    }
}
