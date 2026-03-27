using AvatarChat.Main;
using AvatarChat.Network.Handlers;
using AvatarChat.Network.Signals;
using Unity.Netcode;
using UnityEngine;
using Zenject;
using UniRx;
using System.Linq;

namespace AvatarChat.Network.Test
{
    public class NetworkChatTest : MonoBehaviour
    {
        [Inject] private INetworkHandler _networkHandler;

        private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=";

        private void Update()
        {
            var chatHandler = _networkHandler.GetSubHandler<NetworkChatHandler>();
            if (chatHandler == null) return;

            if (Input.GetKeyDown(KeyCode.T))
            {
                string randomText = GenerateRandomString(Random.Range(5, 15));
                chatHandler.SendTextMessage(randomText);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                int garbageValue = Random.Range(0, 42);
                chatHandler.SendEmojiMessage(garbageValue);
            }
        }

        private string GenerateRandomString(int length)
        {
            return new string(Enumerable.Repeat(Chars, length)
                .Select(s => s[Random.Range(0, s.Length)]).ToArray());
        }
    }
}