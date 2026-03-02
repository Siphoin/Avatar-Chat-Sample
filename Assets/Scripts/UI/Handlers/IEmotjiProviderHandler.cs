using AvatarChat.Network.Models;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AvatarChat.UI.Handlers
{
   public interface IEmotjiProviderHandler
    {
        UniTask<Sprite> GetSpriteForMessage(NetworkMessage message);
    }
}