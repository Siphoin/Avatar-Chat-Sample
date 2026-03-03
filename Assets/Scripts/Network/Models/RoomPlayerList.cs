using Unity.Collections;
using Unity.Netcode;
using System;

namespace AvatarChat.Network.Models
{
    public struct RoomPlayerList : INetworkSerializable, IEquatable<RoomPlayerList>
    {
        public NetworkGuid RoomId;
        public ulong[] PlayerIds;

        public static RoomPlayerList Empty => new RoomPlayerList
        {
            RoomId = default,
            PlayerIds = Array.Empty<ulong>()
        };

        public bool IsEmpty => RoomId.Equals(default);

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoomId);
            
            if (serializer.IsWriter)
            {
                var writer = serializer.GetFastBufferWriter();
                int length = PlayerIds?.Length ?? 0;
                writer.WriteValueSafe(length);
                if (PlayerIds != null)
                {
                    for (int i = 0; i < length; i++)
                    {
                        writer.WriteValueSafe(PlayerIds[i]);
                    }
                }
            }
            else
            {
                var reader = serializer.GetFastBufferReader();
                reader.ReadValueSafe(out int length);
                PlayerIds = new ulong[length];
                for (int i = 0; i < length; i++)
                {
                    reader.ReadValueSafe(out PlayerIds[i]);
                }
            }
        }

        public bool Equals(RoomPlayerList other)
        {
            return RoomId.Equals(other.RoomId);
        }
    }
}
