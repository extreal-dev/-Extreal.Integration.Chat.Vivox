using System;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using UniRx;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenPresenter : IInitializable, IDisposable
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

        public void Dispose()
        {
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
