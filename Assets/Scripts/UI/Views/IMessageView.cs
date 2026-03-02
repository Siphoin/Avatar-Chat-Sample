using AvatarChat.Main;
using AvatarChat.Main.Factories;
using AvatarChat.Network.Models;

namespace AvatarChat.UI.Views
{
    public interface IMessageView : IFactoryObject, IVisibable
    {
        void SetMessage(NetworkMessage message);
    }
}