using System;
using Unity.Collections;
using Unity.Netcode;

namespace AvatarChat.Network.Models
{
    public struct NetworkPlayer : INetworkSerializable, IEquatable<NetworkPlayer>
    {
        public ulong ClientId;
        public FixedString64Bytes Name;
        public NetworkGuid InstanceId;
        public NetworkGuid CurrentRoomId;

        public static NetworkPlayer Empty => new NetworkPlayer
        {
            ClientId = 0,
            Name = default,
            InstanceId = default,
            CurrentRoomId = default
        };

        public bool IsEmpty => Equals(Empty);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref InstanceId);
            serializer.SerializeValue(ref CurrentRoomId);
        }

        public bool Equals(NetworkPlayer other)
        {
            return InstanceId.Equals(other.InstanceId);
        }
    }
}