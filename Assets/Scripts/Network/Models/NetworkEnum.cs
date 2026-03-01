using Unity.Netcode;
using System;

namespace AvatarChat.Network.Models
{
    public struct NetworkEnum<T> : INetworkSerializable, IEquatable<NetworkEnum<T>> where T : unmanaged, Enum
    {
        private T _value;

        public T Value
        {
            get => _value;
            set => _value = value;
        }

        public NetworkEnum(T value)
        {
            _value = value;
        }

        public void NetworkSerialize<T2>(BufferSerializer<T2> serializer) where T2 : IReaderWriter
        {
            serializer.SerializeValue(ref _value);
        }

        public bool Equals(NetworkEnum<T> other) => _value.Equals(other._value);

        public override bool Equals(object obj) => obj is NetworkEnum<T> other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString();

        public static implicit operator T(NetworkEnum<T> networkEnum) => networkEnum._value;

        public static implicit operator NetworkEnum<T>(T value) => new NetworkEnum<T>(value);
    }
}