using Unity.Netcode;
using Unity.Collections;
using System;
using UnityEngine;

namespace AvatarChat.Network.Models
{
    public struct NetworkGuid : INetworkSerializable, IEquatable<NetworkGuid>
    {
        private ulong _first;
        private ulong _second;

        public static NetworkGuid Empty => new NetworkGuid(Guid.Empty);

        public NetworkGuid(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            _first = BitConverter.ToUInt64(bytes, 0);
            _second = BitConverter.ToUInt64(bytes, 8);
        }

        public NetworkGuid(FixedString64Bytes fixedString)
        {
            if (Guid.TryParse(fixedString.ToString(), out Guid guid))
            {
                byte[] bytes = guid.ToByteArray();
                _first = BitConverter.ToUInt64(bytes, 0);
                _second = BitConverter.ToUInt64(bytes, 8);
            }
            else
            {
                Debug.LogError("Invalid GUID format: " + fixedString);
                _first = 0;
                _second = 0;
            }
        }

        public Guid ToGuid()
        {
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(_first), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(_second), 0, bytes, 8, 8);
            return new Guid(bytes);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _first);
            serializer.SerializeValue(ref _second);
        }

        public bool Equals(NetworkGuid other) => _first == other._first && _second == other._second;
        public override string ToString() => ToGuid().ToString();

        public static implicit operator NetworkGuid(Guid guid) => new NetworkGuid(guid);
        public static implicit operator Guid(NetworkGuid netGuid) => netGuid.ToGuid();
        public static implicit operator NetworkGuid(FixedString64Bytes fixedString) => new NetworkGuid(fixedString);
    }
}