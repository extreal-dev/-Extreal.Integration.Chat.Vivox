using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using UniRx;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatScreenPresenter : IInitializable, IDisposable
    {
        private readonly TextChatScreenView textChatScreenView;
        private readonly TextChatScreenModel textChatScreenModel;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public TextChatScreenPresenter
        (
            TextChatScreenView textChatScreenView,
            TextChatScreenModel textChatScreenModel
        )
        {
            this.textChatScreenView = textChatScreenView;
            this.textChatScreenModel = textChatScreenModel;
        }

        public void Initialize()
        {
            textChatScreenModel.Initialize();

            textChatScreenView.OnSendButtonClicked
                .Subscribe(textChatScreenModel.SendTextMessage)
                .AddTo(disposables);

            textChatScreenModel.OnTextMessageReceived
                .Subscribe(textChatScreenView.ShowMessage)
                .AddTo(disposables);
        }

        public void Dispose()
        {
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
