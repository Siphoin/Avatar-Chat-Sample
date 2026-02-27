using AvatarChat.Network.Models;
using Unity.Collections;
using Unity.Netcode;

namespace AvatarChat.Network.Handlers
{
    public interface INetworkRoomHandler : ISubNetworkHandler
    {
        NetworkList<NetworkRoom> ActiveRooms { get; }
        void CreateRoom(FixedString64Bytes roomName, int maxPlayers);
   
        void JoinRoom(FixedString64Bytes instanceId);
    }
}