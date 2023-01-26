using System;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.System;
using UniRx;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatScreenModel : DisposableBase
    {
        public IObservable<string> OnTextMessageReceived
            => vivoxClient.OnTextMessageReceived.Select(receivedMessage => receivedMessage.Message);

        private readonly VivoxClient vivoxClient;

        private ChannelId activeChannelId;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public TextChatScreenModel(VivoxClient vivoxClient)
            => this.vivoxClient = vivoxClient;

        public void Initialize()
            => vivoxClient.OnChannelSessionAdded
                .Subscribe(channelId => activeChannelId = channelId)
                .AddTo(disposables);

        protected override void ReleaseManagedResources() => disposables.Dispose();

        public void SendTextMessage(string message)
            => vivoxClient.SendTextMessage(message, activeChannelId);
    }
}
