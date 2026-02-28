using Unity.Collections;
using Unity.Netcode;
using System;

namespace AvatarChat.Network.Models
{
    public struct NetworkRoom : INetworkSerializable, IEquatable<NetworkRoom>
    {
        public FixedString128Bytes RoomName;
        public NetworkGuid InstanceId;
        public int MaxPlayers;
        public int CurrentPlayers;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoomName);
            serializer.SerializeValue(ref InstanceId);
            serializer.SerializeValue(ref MaxPlayers);
            serializer.SerializeValue(ref CurrentPlayers);
        }

        public bool Equals(NetworkRoom other)
        {
            return InstanceId.Equals(other.InstanceId);
        }
    }
}