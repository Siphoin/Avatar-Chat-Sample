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
        [Inject] protected INetworkHandler _handler;

        private NetworkVariable<NetworkGuid> _roomGuid = new(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        protected Unity.Netcode.NetworkObject _networkObject;

        public NetworkPlayer Owner => _handler.GetSubHandler<NetworkPlayerHandler>().GetPlayer(OwnerClientId);
        public NetworkRoom Room => _handler.GetSubHandler<NetworkRoomHandler>().GetRoom(_roomGuid.Value);

        public override void OnNetworkSpawn()
        {
            _networkObject = GetComponent<Unity.Netcode.NetworkObject>();

            if (IsServer)
            {
                var roomHandler = _handler.GetSubHandler<NetworkRoomHandler>();
                var spawnHandler = _handler.GetSubHandler<NetworkSpawnHandler>();

                NetworkRoom targetRoom = roomHandler.GetRoomByPlayer(OwnerClientId);
                if (!targetRoom.IsEmpty)
                {
                    SetRoom(targetRoom);
                    spawnHandler.TrackPlayerRoom(OwnerClientId, targetRoom.InstanceId);
                }

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                _roomGuid.OnValueChanged += OnRoomChanged;

                CheckObjectVisibility();
            }

            InitializePlayerAsync().Forget();
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                _roomGuid.OnValueChanged -= OnRoomChanged;
            }
        }

        private void OnRoomChanged(NetworkGuid prev, NetworkGuid next) => CheckObjectVisibility();
        private void OnClientConnected(ulong id) => CheckObjectVisibility();

        public void CheckObjectVisibility()
        {
            if (!IsServer) return;

            var spawnHandler = _handler.GetSubHandler<NetworkSpawnHandler>();
            var playersInMyRoom = spawnHandler.GetPlayersInRoom(_roomGuid.Value);

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (playersInMyRoom.Contains(clientId))
                    _networkObject.NetworkShow(clientId);
                else
                    _networkObject.NetworkHide(clientId);
            }
        }

        private async UniTaskVoid InitializePlayerAsync()
        {
            await UniTask.WaitUntil(() => !Owner.IsEmpty && !Room.IsEmpty,
                cancellationToken: this.GetCancellationTokenOnDestroy());
            OnPlayerReady();
        }

        protected virtual void OnPlayerReady() { }

        internal void SetRoom(NetworkRoom room)
        {
            if (IsServer && !room.IsEmpty) _roomGuid.Value = room.InstanceId;
        }

        public void GiveOwnership(ulong clientId) { if (IsServer) _networkObject.ChangeOwnership(clientId); }
        public void ResetOwnership() { if (IsServer) _networkObject.RemoveOwnership(); }
    }
}