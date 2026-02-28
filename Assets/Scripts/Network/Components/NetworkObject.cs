using AvatarChat.Network.Handlers;
using AvatarChat.Network.Models;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace AvatarChat.Network.Components
{
    [RequireComponent(typeof(ZenAutoInjecter))]
    public abstract class NetworkObject : NetworkBehaviour
    {
        [Inject] private INetworkHandler _handler;

        private Unity.Netcode.NetworkObject _networkObject;

        public NetworkPlayer Owner
        {
            get
            {
                var playerHandler = _handler.GetSubHandler<NetworkPlayerHandler>();
                return playerHandler.GetPlayer(OwnerClientId);
            }
        }

        public override void OnNetworkSpawn()
        {
            _networkObject = GetComponent<Unity.Netcode.NetworkObject>();

            InitializePlayerAsync().Forget();
        }

        private async UniTaskVoid InitializePlayerAsync()
        {
            await UniTask.WaitUntil(() => !Owner.IsEmpty,
                cancellationToken: this.GetCancellationTokenOnDestroy());

            OnPlayerReady();
        }

        protected virtual void OnPlayerReady()
        {

        }

        public void GiveOwnership(ulong clientId)
        {
            if (IsServer)
            {
                _networkObject.ChangeOwnership(clientId);
            }
        }

        public void ResetOwnership()
        {
            if (IsServer)
            {
                _networkObject.RemoveOwnership();
            }
        }
    }
}