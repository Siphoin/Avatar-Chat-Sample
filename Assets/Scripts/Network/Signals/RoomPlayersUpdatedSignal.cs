using AvatarChat.Network.Models;

namespace AvatarChat.Network.Signals
{
    public class RoomPlayersUpdatedSignal
    {
        public readonly NetworkGuid RoomId;

        public RoomPlayersUpdatedSignal(NetworkGuid roomId)
        {
            RoomId = roomId;
        }
    }
}