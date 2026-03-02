using AvatarChat.Main.Factories;
using AvatarChat.UI.Views;

namespace AvatarChat.UI.Factories
{
    public interface IMessageViewFactory : IMonoBehaviorFactory<MessageViewBase, IMessageView>
    {
    }
}