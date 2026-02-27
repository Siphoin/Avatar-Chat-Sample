using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace AvatarChat.Network.Handlers
{
    [RequireComponent(typeof(ZenAutoInjecter))]
    public abstract class SubNetworkHandler : NetworkBehaviour, ISubNetworkHandler
    {
    }
}