using Unity.Netcode;

namespace AvatarChat.Network.Handlers
{
    public interface INetworkHandler
    {
        void StartHost();
        void StartClient();
        void StartServer();
        void Shutdown();
        public T GetSubHandler<T>();
    }
}