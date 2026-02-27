namespace AvatarChat.Network.Signals
{
    public class PlayerLeftSignal
    {
        public ulong ClientId { get; private set; }

        public PlayerLeftSignal(ulong clientId)
        {
            ClientId = clientId;
        }
    }
}