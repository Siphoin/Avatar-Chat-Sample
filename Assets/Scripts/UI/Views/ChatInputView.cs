
using AvatarChat.Core.InputSystem;
using AvatarChat.Network.Handlers;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

namespace AvatarChat.UI.Views
{
    public class ChatInputView : MonoBehaviour, IDisposable
    {
        [SerializeField, ReadOnly] private TMP_InputField _inputField;
        [SerializeField] private Button _sendButton;
        
        [Inject] private INetworkHandler _networkHandler;
        [Inject] private Core.InputSystem.IInputSystem _inputSystem;

        private bool _isInputActive;
        private int _historyIndex = -1;
        private string _originalText = string.Empty;

        private void Awake()
        {
            if (_sendButton != null)
            {
                _sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (_inputField != null)
            {
                _inputField.onEndEdit.AddListener(OnInputEndEdit);
                _inputField.onValueChanged.AddListener(OnInputValueChanged);
                _inputField.onSelect.AddListener(OnInputFocused);
                _inputField.onDeselect.AddListener(OnInputLostFocus);
            }

            _inputSystem.AddListener(OnKeyDown, StandaloneInputEventType.KeyDown, true);
        }

        private void OnDestroy()
        {
            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveListener(OnSendButtonClicked);
            }

            if (_inputField != null)
            {
                _inputField.onEndEdit.RemoveListener(OnInputEndEdit);
                _inputField.onValueChanged.RemoveListener(OnInputValueChanged);
                _inputField.onSelect.RemoveListener(OnInputFocused);
                _inputField.onDeselect.RemoveListener(OnInputLostFocus);
            }

            _inputSystem.RemoveListener(OnKeyDown, StandaloneInputEventType.KeyDown);
        }

        private void OnInputLostFocus(string arg0)
        {
            _isInputActive = false;
        }

        private void OnInputFocused(string arg0)
        {
            _isInputActive = true;
        }

        private void OnKeyDown(KeyCode keyCode)
        {
            Debug.Log(keyCode);
            switch (keyCode)
            {

                case KeyCode.Return or KeyCode.KeypadEnter when _isInputActive && _inputField?.isFocused == true:
                    SendCurrentMessage();
                    break;
                case KeyCode.UpArrow when _isInputActive:
                    NavigateHistory(1);
                    break;

                case KeyCode.DownArrow when _isInputActive:
                    NavigateHistory(-1);
                    break;
            }
        }

        private void OnInputValueChanged(string newText)
        {
            if (string.IsNullOrEmpty(newText))
            {
                _historyIndex = -1;
            }
        }

        private void NavigateHistory(int direction)
        {
            var chatHandler = _networkHandler.GetSubHandler<NetworkChatHandler>();
            if (chatHandler == null) return;

            int historyCount = chatHandler.GetHistoryCount();
            if (historyCount == 0) return;

            if (_historyIndex == -1)
            {
                _originalText = _inputField.text;
            }

            _historyIndex += direction;

            if (_historyIndex < -1)
            {
                _historyIndex = -1;
                _inputField.text = _originalText;
                _inputField.caretPosition = _inputField.text.Length;
                return;
            }

            if (_historyIndex >= historyCount)
            {
                _historyIndex = historyCount - 1;
            }

            if (_historyIndex == -1)
            {
                _inputField.text = _originalText;
            }
            else
            {
                string message = chatHandler.GetMessageTextByIndex(_historyIndex);
                if (!string.IsNullOrEmpty(message))
                {
                    _inputField.text = message;
                    _inputField.caretPosition = _inputField.text.Length;
                }
            }
        }

        private void OnSendButtonClicked()
        {
            SendCurrentMessage();
        }

        private void OnInputEndEdit(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            SendCurrentMessage();
        }

        private void SendCurrentMessage()
        {
            if (_inputField == null) return;

            string message = _inputField.text.Trim();

            if (!string.IsNullOrEmpty(message))
            {
                var chatHandler = _networkHandler.GetSubHandler<NetworkChatHandler>();
                chatHandler.SendTextMessage(message);
            }

            _inputField.text = string.Empty;
            _historyIndex = -1;
            _originalText = string.Empty;
            _inputField.ActivateInputField();
        }

        public void Dispose()
        {
            _inputSystem.RemoveListener(OnKeyDown, StandaloneInputEventType.KeyDown);
        }

        private void OnValidate()
        {
            if (!_inputField)
            {
                _inputField = GetComponent<TMP_InputField>();
            }
        }
    }
}
