namespace AvatarChat.Network.Signals
{
    public class ConnectionApprovedSignal
    {
        public ulong ClientId { get; private set; }
        public string PlayerName { get; private set; }

        public ConnectionApprovedSignal(ulong clientId, string playerName)
        {
            ClientId = clientId;
            PlayerName = playerName;
        }
    }
}
