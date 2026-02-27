namespace AvatarChat.Network.Signals
{
    public class RoomRemovedSignal
    {
        public string InstanceId { get; private set; }

        public RoomRemovedSignal(string instanceId)
        {
            InstanceId = instanceId;
        }
    }
}