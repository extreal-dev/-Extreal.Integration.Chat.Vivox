using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using UniRx;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenModel : DisposableBase
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

        protected override void ReleaseManagedResources()
        {
            onMuted.Dispose();
            disposables.Dispose();
        }

        public void ToggleMute()
        {
            vivoxClient.Client.AudioInputDevices.Muted ^= true;
            onMuted.Value = vivoxClient.Client.AudioInputDevices.Muted ? "OFF" : "ON";
        }
    }
}
