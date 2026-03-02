using System;
using SouthPointe.Serialization.MessagePack;
using Unity.Netcode;

namespace AvatarChat.Network.Models
{
    [Serializable]
    public struct NetworkMessage : INetworkSerializable, IEquatable<NetworkMessage>
    {
        public ulong OwnerClientId;
        public MessageType Type;
        public byte[] Data;
        public NetworkGuid InstanceId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref OwnerClientId);
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref Data);

            serializer.SerializeValue(ref InstanceId);
        }

        public bool Equals(NetworkMessage other)
        {
            return InstanceId.Equals(other.InstanceId);
        }

        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }
    }
}