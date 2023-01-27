using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatScreenPresenter : DisposableBase, IInitializable
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

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
