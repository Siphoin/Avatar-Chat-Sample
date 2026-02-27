namespace AvatarChat.Network.Signals
{
    public class PlayerJoinedRoomSignal
    {
        public ulong ClientId { get; private set; }
        public string InstanceId { get; private set; }

        public PlayerJoinedRoomSignal(ulong clientId, string instanceId)
        {
            ClientId = clientId;
            InstanceId = instanceId;
        }
    }
}