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

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            MessagePackFormatter formatter = new();

            if (serializer.IsWriter)
            {
                byte[] packed = formatter.Serialize(this);
                int len = packed.Length;
                serializer.SerializeValue(ref len);
                serializer.SerializeValue(ref packed);
            }
            else
            {
                int len = 0;
                serializer.SerializeValue(ref len);
                byte[] packed = new byte[len];
                serializer.SerializeValue(ref packed);
                this = formatter.Deserialize<NetworkMessage>(packed);
            }
        }

        public bool Equals(NetworkMessage other)
        {
            return OwnerClientId == other.OwnerClientId &&
                   Type == other.Type &&
                   Data == other.Data;
        }
    }
}