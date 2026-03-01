using AvatarChat.Network.Models;

namespace AvatarChat.Network.Signals
{
    public class DestroyChatMessageSignal
    {
        public NetworkMessage Message { get; private set; }
        public DestroyChatMessageSignal(NetworkMessage message) => Message = message;
    }
}