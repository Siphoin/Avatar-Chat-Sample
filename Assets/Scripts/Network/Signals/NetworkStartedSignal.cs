namespace AvatarChat.Network.Signals
{
    public class NetworkStartedSignal
    {
        public bool IsServer { get; }
        public bool IsHost { get; }
        public ulong LocalClientId { get; }

        public NetworkStartedSignal(bool isServer, bool isHost, ulong localClientId)
        {
            IsServer = isServer;
            IsHost = isHost;
            LocalClientId = localClientId;
        }
    }
}