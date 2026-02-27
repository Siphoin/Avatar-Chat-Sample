namespace AvatarChat.Network.Signals
{
    public class RoomCreatedSignal
    {
        public string InstanceId { get; private set; }
        public string RoomName { get; private set; }

        public RoomCreatedSignal(string instanceId, string roomName)
        {
            InstanceId = instanceId;
            RoomName = roomName;
        }
    }
}