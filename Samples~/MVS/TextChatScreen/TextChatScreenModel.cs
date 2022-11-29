using System;
using Cysharp.Threading.Tasks;
using UniRx;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatScreenModel : IDisposable
    {
        public IObservable<string> OnTextMessageReceived
            => vivoxClient.OnTextMessageReceived.Select(receivedMessage => receivedMessage.ReceivedValue);

        private readonly VivoxClient vivoxClient;

        private ChannelId activeChannelId;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public TextChatScreenModel(VivoxClient vivoxClient)
            => this.vivoxClient = vivoxClient;

        public void Initialize()
            => vivoxClient.OnChannelSessionAdded
                .Subscribe(channelId => activeChannelId = channelId)
                .AddTo(disposables);

        public void Dispose()
        {
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }

        public void SendTextMessage(string message)
            => vivoxClient.SendTextMessage(message, activeChannelId);
    }
}
