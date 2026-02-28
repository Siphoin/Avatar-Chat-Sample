using Unity.Collections;
using Unity.Netcode;
using System;

namespace AvatarChat.Network.Models
{
    public struct NetworkPlayer : INetworkSerializable, IEquatable<NetworkPlayer>
    {
        public ulong ClientId;
        public FixedString64Bytes Name;
        public NetworkGuid InstanceId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref InstanceId);
        }

        public bool Equals(NetworkPlayer other)
        {
            return InstanceId.Equals(other.InstanceId);
        }
    }
}