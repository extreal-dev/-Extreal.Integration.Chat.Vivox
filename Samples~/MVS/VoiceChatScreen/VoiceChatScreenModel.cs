using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenModel : IDisposable
    {
        public IReadOnlyReactiveProperty<bool> OnMuted => onMuted;
        private readonly BoolReactiveProperty onMuted = new BoolReactiveProperty(true);

        private readonly VivoxClient vivoxClient;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public VoiceChatScreenModel(VivoxClient vivoxClient)
            => this.vivoxClient = vivoxClient;

        public void Initialize()
            => vivoxClient.OnLoggedIn
                .Subscribe(_ => vivoxClient.MuteInputDevice(true))
                .AddTo(disposables);

        public void Dispose()
        {
            onMuted.Dispose();
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }

        public void ToggleMute()
        {
            onMuted.Value = !onMuted.Value;
            vivoxClient.MuteInputDevice(onMuted.Value);
        }
    }
}
