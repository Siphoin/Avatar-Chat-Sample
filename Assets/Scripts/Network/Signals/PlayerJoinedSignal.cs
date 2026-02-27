namespace AvatarChat.Network.Signals
{
    public class PlayerJoinedSignal
    {
        public ulong ClientId { get; private set; }

        public PlayerJoinedSignal(ulong clientId)
        {
            ClientId = clientId;
        }
    }
}