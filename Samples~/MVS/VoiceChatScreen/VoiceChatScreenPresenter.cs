using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using UniRx;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenPresenter : DisposableBase, IInitializable
    {
        private readonly VoiceChatScreenView voiceChatScreenView;
        private readonly VoiceChatScreenModel voiceChatScreenModel;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public VoiceChatScreenPresenter
        (
            VoiceChatScreenView voiceChatScreenView,
            VoiceChatScreenModel voiceChatScreenModel
        )
        {
            this.voiceChatScreenView = voiceChatScreenView;
            this.voiceChatScreenModel = voiceChatScreenModel;
        }

        public void Initialize()
        {
            voiceChatScreenModel.Initialize();

            voiceChatScreenView.OnMuteButtonClicked
                .Subscribe(_ => voiceChatScreenModel.ToggleMute())
                .AddTo(disposables);

            voiceChatScreenModel.OnMuted
                .Subscribe(voiceChatScreenView.SetMutedString)
                .AddTo(disposables);
        }

        protected override void ReleaseManagedResources() => disposables.Dispose();
    }
}
