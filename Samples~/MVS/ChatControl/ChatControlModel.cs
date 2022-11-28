using System;
using Cysharp.Threading.Tasks;
using Extreal.Integration.Chat.Vivox.MVS.App;
using UniRx;

namespace Extreal.Integration.Chat.Vivox.MVS.ChatControl
{
    public class ChatControlModel : IDisposable
    {
        private readonly VivoxClient vivoxClient;
        private readonly VivoxConnectionConfig vivoxConnectionConfig;

        private readonly CompositeDisposable disposables = new CompositeDisposable();

        public ChatControlModel(VivoxClient vivoxClient, VivoxConnectionConfig vivoxConnectionConfig)
        {
            this.vivoxClient = vivoxClient;
            this.vivoxConnectionConfig = vivoxConnectionConfig;
        }

        public void Initialize()
        {
            vivoxClient.InitializeAsync(vivoxConnectionConfig).Forget();

            vivoxClient.OnLoggedIn
                .Subscribe(_ =>
                {
                    var vivoxConnectionParameter = new VivoxConnectionParameter("GuestChannel");
                    vivoxClient.Connect(vivoxConnectionParameter);
                })
                .AddTo(disposables);
        }

        public void Dispose()
        {
            disposables.Dispose();
            GC.SuppressFinalize(this);
        }

        public void OnStageTransitioned(StageName stageName)
        {
            if (AppUtils.IsSpace(stageName))
            {
                var vivoxLoginParameter = new VivoxLoginParameter("Guest");
                vivoxClient.Login(vivoxLoginParameter);
            }
            else
            {
                vivoxClient.DisconnectAllChannels();
                vivoxClient.Logout();
            }
        }
    }
}
