using System;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenModel : IDisposable
    {
        public IReadOnlyReactiveProperty<string> OnMuted => onMuted;
        private readonly ReactiveProperty<string> onMuted = new ReactiveProperty<string>("OFF");

        private readonly VivoxClient vivoxClient;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public VoiceChatScreenModel(VivoxClient vivoxClient)
            => this.vivoxClient = vivoxClient;

        public void Initialize()
            => vivoxClient.OnLoggedIn
                .Subscribe(_ => vivoxClient.Client.AudioInputDevices.Muted = true)
                .AddTo(disposables);

        public void Dispose()
        {
            onMuted.Dispose();
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }

        public void ToggleMute()
        {
            vivoxClient.Client.AudioInputDevices.Muted ^= true;
            onMuted.Value = vivoxClient.Client.AudioInputDevices.Muted ? "OFF" : "ON";
        }
    }
}
