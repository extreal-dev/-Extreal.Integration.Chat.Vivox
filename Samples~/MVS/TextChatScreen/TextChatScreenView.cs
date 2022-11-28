using System;
using UnityEngine;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatScreenView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Transform messageRoot;
        [SerializeField] private GameObject textChatPrefab;

        public IObservable<string> OnSendButtonClicked => onSendButtonClicked;
        private readonly Subject<string> onSendButtonClicked = new Subject<string>();

        private void Awake()
            => sendButton.OnClickAsObservable()
                .TakeUntilDestroy(this)
                .Subscribe(_ =>
                {
                    onSendButtonClicked.OnNext(inputField.text);
                });

        private void OnDestroy()
            => onSendButtonClicked.Dispose();

        public void ShowMessage(string message)
            => Instantiate(textChatPrefab, messageRoot)
                .GetComponent<TextChatMonobehaviour>()
                .SetText(message);
    }
}
