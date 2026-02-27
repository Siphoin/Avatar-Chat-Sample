using AvatarChat.Network.Models;
using Unity.Collections;
using Unity.Netcode;

namespace AvatarChat.Network.Handlers
{
    public interface INetworkRoomHandler : ISubNetworkHandler
    {
        NetworkList<NetworkRoom> ActiveRooms { get; }

        void RequestCreateRoom(FixedString64Bytes roomName, int maxPlayers);

        void RequestJoinRoom(FixedString64Bytes instanceId);

        void RequestLeaveRoom(FixedString64Bytes instanceId);

        void RemoveRoom(FixedString64Bytes instanceId);
    }
}