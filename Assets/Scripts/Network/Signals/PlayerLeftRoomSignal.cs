namespace AvatarChat.Network.Signals
{
    public class PlayerLeftRoomSignal
    {
        public ulong ClientId { get; private set; }
        public string InstanceId { get; private set; }

        public PlayerLeftRoomSignal(ulong clientId, string instanceId)
        {
            ClientId = clientId;
            InstanceId = instanceId;
        }
    }
}