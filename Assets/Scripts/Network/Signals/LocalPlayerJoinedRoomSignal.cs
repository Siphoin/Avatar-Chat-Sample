namespace AvatarChat.Network.Signals
{
    public class LocalPlayerJoinedRoomSignal
    {
        public string InstanceId { get; private set; }
        public string RoomName { get; private set; }

        public LocalPlayerJoinedRoomSignal(string instanceId, string roomName)
        {
            InstanceId = instanceId;
            RoomName = roomName;
        }
    }

    public class LocalPlayerLeftRoomSignal
    {
        public string InstanceId { get; private set; }

        public LocalPlayerLeftRoomSignal(string instanceId)
        {
            InstanceId = instanceId;
        }
    }
}