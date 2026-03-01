using AvatarChat.Network.Handlers;
using AvatarChat.Network.Models;
using AvatarChat.Network.Signals;
using AvatarChat.Main;
using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using UniRx;

namespace AvatarChat.Network.Components
{
    [RequireComponent(typeof(ZenAutoInjecter))]
    public abstract class NetworkObject : NetworkBehaviour
    {
        [Inject] protected INetworkHandler _handler;
        [Inject] private SignalBus _signalBus;

        private NetworkVariable<NetworkGuid> _roomGuid = new(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);
        protected Unity.Netcode.NetworkObject _networkObject;
        private IDisposable _roomUpdateSubscription;

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

                _roomUpdateSubscription = _signalBus.GetStream<RoomPlayersUpdatedSignal>()
                    .Where(sig => sig.RoomId.Equals(_roomGuid.Value))
                    .Subscribe(_ => CheckObjectVisibility());

                _roomGuid.OnValueChanged += OnRoomChanged;
                CheckObjectVisibility();
            }
            else
            {
                _roomGuid.OnValueChanged += OnRoomChanged;
                MoveToRoomScene(_roomGuid.Value);
            }

            InitializePlayerAsync().Forget();
        }

        public override void OnNetworkDespawn()
        {
            _roomGuid.OnValueChanged -= OnRoomChanged;
            _roomUpdateSubscription?.Dispose();
        }

        private void OnRoomChanged(NetworkGuid prev, NetworkGuid next)
        {
            if (IsServer) CheckObjectVisibility();
            MoveToRoomScene(next);
        }

        public void CheckObjectVisibility()
        {
            if (!IsServer || !IsSpawned || NetworkManager.Singleton == null)
                return;

            var spawnHandler = _handler.GetSubHandler<NetworkSpawnHandler>();
            var playersInMyRoom = spawnHandler.GetPlayersInRoom(_roomGuid.Value);

            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                bool shouldShow = playersInMyRoom.Contains(clientId);
                bool currentlyVisible = _networkObject.IsNetworkVisibleTo(clientId);

                if (shouldShow && !currentlyVisible)
                {
                    _networkObject.NetworkShow(clientId);
                }
                else if (!shouldShow && currentlyVisible)
                {
                    _networkObject.NetworkHide(clientId);
                }
            }
        }

        private async UniTaskVoid InitializePlayerAsync()
        {
            var roomHandler = _handler.GetSubHandler<NetworkRoomHandler>();

            await UniTask.WaitUntil(() => !Owner.IsEmpty && !Room.IsEmpty,
                cancellationToken: this.GetCancellationTokenOnDestroy());

            Scene targetScene = default;
            await UniTask.WaitUntil(() =>
            {
                targetScene = roomHandler.GetRoomScene(_roomGuid.Value);
                return targetScene.IsValid() && targetScene.isLoaded;
            }, cancellationToken: this.GetCancellationTokenOnDestroy());

            MoveToRoomScene(_roomGuid.Value);
            OnPlayerReady();
        }

        protected virtual void OnPlayerReady() { }

        internal void SetRoom(NetworkRoom room)
        {
            if (IsServer && !room.IsEmpty)
            {
                _roomGuid.Value = room.InstanceId;
                MoveToRoomScene(room.InstanceId);
            }
        }

        private void MoveToRoomScene(NetworkGuid roomId)
        {
            if (roomId.Equals(new NetworkGuid())) return;

            var roomHandler = _handler.GetSubHandler<NetworkRoomHandler>();
            Scene targetScene = roomHandler.GetRoomScene(roomId);

            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, targetScene);
            }
        }

        public void GiveOwnership(ulong clientId) { if (IsServer) _networkObject.ChangeOwnership(clientId); }
        public void ResetOwnership() { if (IsServer) _networkObject.RemoveOwnership(); }
    }
}