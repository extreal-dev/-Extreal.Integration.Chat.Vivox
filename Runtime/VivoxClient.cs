using System;
using System.ComponentModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using UniRx;
using UnityEngine;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox
{
    public class VivoxClient : IDisposable
    {
        public IObservable<Unit> OnLoggedIn => onLoggedIn.AddTo(disposables);
        private readonly Subject<Unit> onLoggedIn = new Subject<Unit>();

        public IObservable<Unit> OnLoggedOut => onLoggedOut.AddTo(disposables);
        private readonly Subject<Unit> onLoggedOut = new Subject<Unit>();

        public IObservable<ConnectionRecoveryState> OnRecoveryStateChanged => onRecoveryStateChanged.AddTo(disposables);
        private readonly Subject<ConnectionRecoveryState> onRecoveryStateChanged
            = new Subject<ConnectionRecoveryState>();

        public IObservable<ChannelId> OnChannelSessionAdded => onChannelSessionAdded.AddTo(disposables);
        private readonly Subject<ChannelId> onChannelSessionAdded = new Subject<ChannelId>();

        public IObservable<ChannelId> OnChannelSessionRemoved => onChannelSessionRemoved.AddTo(disposables);
        private readonly Subject<ChannelId> onChannelSessionRemoved = new Subject<ChannelId>();

        public IObservable<IParticipant> OnUserConnected => onUserConnected.AddTo(disposables);
        private readonly Subject<IParticipant> onUserConnected = new Subject<IParticipant>();

        public IObservable<IParticipant> OnUserDisconnected => onUserDisconnected.AddTo(disposables);
        private readonly Subject<IParticipant> onUserDisconnected = new Subject<IParticipant>();

        public IObservable<VivoxReceivedValue<string>> OnTextMessageReceived => onTextMessageReceived.AddTo(disposables);
        private readonly Subject<VivoxReceivedValue<string>> onTextMessageReceived
            = new Subject<VivoxReceivedValue<string>>();

        public IObservable<VivoxReceivedValue<double>> OnAudioEnergyChanged => onAudioEnergyChanged.AddTo(disposables);
        private readonly Subject<VivoxReceivedValue<double>> onAudioEnergyChanged
            = new Subject<VivoxReceivedValue<double>>();

        public Client Client { get; private set; }
        public ILoginSession LoginSession { get; private set; }

        private bool IsLoggingIn => LoginSession?.State == LoginState.LoggingIn;
        private bool IsLoggedIn => LoginSession?.State == LoginState.LoggedIn;

        private IChannelSession positionalChannelSession;
        private IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannelSessions
            => LoginSession?.ChannelSessions;

        private readonly VivoxAppConfig config;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(VivoxClient));

        public VivoxClient(VivoxAppConfig config)
        {
            if (!CheckManualCredentials(config))
            {
                throw new ArgumentNullException(nameof(config), $"'{nameof(config)}' or some value in it is null");
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Initialize {nameof(VivoxClient)}");
            }

            this.config = config;

            Client = new Client(new Uri(config.ApiEndPoint));
            Client.Initialize();
        }

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(VivoxClient)}");
            }

            disposables.Dispose();

            if (LoginSession != null)
            {
                foreach (var channelSession in LoginSession.ChannelSessions)
                {
                    channelSession.PropertyChanged -= OnChannelPropertyChanged;
                    channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
                    channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
                    channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
                    channelSession.MessageLog.AfterItemAdded -= OnMessageLogReceived;
                }

                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                LoginSession.ChannelSessions.AfterKeyAdded -= OnChannelSessionAddedEventHandler;
                LoginSession.ChannelSessions.BeforeKeyRemoved -= OnChannelSessionRemovedEventHandler;
                LoginSession = null;
            }

            Client.Cleanup();
            if (Client != null)
            {
                Client.Uninitialize();
            }

            GC.SuppressFinalize(this);
        }

        public void Login(VivoxAuthConfig authConfig)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This computer is not connected to the Internet");
                }
                return;
            }
            if (IsLoggingIn || IsLoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client already logging/logged into the server");
                }
                return;
            }

            var issuer = config.Issuer;
            var accountName = authConfig.AccountName;
            var domain = config.Domain;
            var displayName = authConfig.DisplayName;
            var accountId = new AccountId(issuer, accountName, domain, displayName);
            var tokenSigningKey = config.TokenKey;
            var tokenExpirationDuration = authConfig.TokenExpirationDuration;

            LoginSession = Client.GetLoginSession(accountId);
            var loginToken = LoginSession.GetLoginToken(tokenSigningKey, TimeSpan.FromSeconds(tokenExpirationDuration));

            LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            LoginSession.ChannelSessions.AfterKeyAdded += OnChannelSessionAddedEventHandler;
            LoginSession.ChannelSessions.BeforeKeyRemoved += OnChannelSessionRemovedEventHandler;
            _ = LoginSession.BeginLogin(loginToken, SubscriptionMode.Accept, null, null, null, LoginSession.EndLogin);
        }

        public void Logout()
        {
            if (!IsLoggingIn && !IsLoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client already logged out of the server");
                }
                return;
            }

            LoginSession.Logout();
        }

        public void Connect(VivoxChannelConfig channelConfig)
        {
            if (!IsLoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to connect before login");
                }
                return;
            }

            var issuer = config.Issuer;
            var channelName = channelConfig.ChannelName;
            var domain = config.Domain;
            var channelType = channelConfig.ChannelType;
            var property = channelConfig.Properties;
            var tokenSigningKey = config.TokenKey;
            var tokenExpirationDuration = channelConfig.TokenExpirationDuration;
            var chatCapability = channelConfig.ChatType;
            var transmissionSwitch = channelConfig.TransmissionSwitch;

            var channel = new ChannelId(issuer, channelName, domain, channelType, property);
            var channelSession = LoginSession.GetChannelSession(channel);
            if (channelSession.ChannelState != ConnectionState.Disconnected)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"This client already connected to the channel '{channelName}'");
                }
                return;
            }

            channelSession.PropertyChanged += OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
            channelSession.MessageLog.AfterItemAdded += OnMessageLogReceived;

            var connectionToken = channelSession.GetConnectToken(tokenSigningKey, TimeSpan.FromSeconds(tokenExpirationDuration));
            _ = channelSession.BeginConnect(chatCapability != ChatType.TextOnly, chatCapability != ChatType.AudioOnly, transmissionSwitch, connectionToken, channelSession.EndConnect);
        }

        public void Disconnect(ChannelId channelId)
        {
            if (ChannelId.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (!IsLoggedIn || !ActiveChannelSessions.ContainsKey(channelId))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"This client has not connected to the channel '{channelId.Name}' yet");
                }
                return;
            }

            var channelSession = ActiveChannelSessions[channelId];
            _ = channelSession.Disconnect();
        }

        public void DisconnectAllChannels()
        {
            if (!IsLoggedIn || ActiveChannelSessions.Count == 0)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client has already disconnected from all channels");
                }
                return;
            }

            var activeChannelIds = ActiveChannelSessions.Keys.ToArray();
            foreach (var activeChannelId in activeChannelIds)
            {
                Disconnect(activeChannelId);
            }
        }

        public void SendTextMessage
        (
            string message,
            ChannelId[] channelIds,
            string language = default,
            string applicationStanzaNamespace = default,
            string applicationStanzaBody = default
        )
        {
            if (channelIds == null)
            {
                throw new ArgumentNullException(nameof(channelIds));
            }

            foreach (var channelId in channelIds)
            {
                _ = SendTextMessage(message, channelId, language, applicationStanzaNamespace, applicationStanzaBody);
            }
        }

        public bool SendTextMessage
        (
            string message,
            ChannelId channelId,
            string language = default,
            string applicationStanzaNamespace = default,
            string applicationStanzaBody = default
        )
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (ChannelId.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (!IsLoggedIn || !ActiveChannelSessions.ContainsKey(channelId))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Unable to send a message before connecting to the channel '{channelId.Name}'");
                }
                return false;
            }

            var channelSession = ActiveChannelSessions[channelId];
            _ = channelSession.BeginSendText(language, message, applicationStanzaNamespace, applicationStanzaBody, channelSession.EndSendText);

            return true;
        }

        public void SetTransmissionMode(TransmissionMode mode, ChannelId channelId = default)
        {
            if (!IsLoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to set the transmission mode before login");
                }
                return;
            }

            if (mode != TransmissionMode.Single)
            {
                LoginSession.SetTransmissionMode(mode);
                return;
            }

            if (ChannelId.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId), "Expects the ID of the channel to be set in single transmission mode");
            }

            if (!ActiveChannelSessions.ContainsKey(channelId))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"Unable to set transmission mode to 'Single' before connecting to the channel '{channelId.Name}'");
                }
                return;
            }

            LoginSession.SetTransmissionMode(mode, channelId);
        }

        public void AdjustInputVolume(int value)
        {
            if (value is < (-50) or > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Enter an integer value between -50 and 50");
            }

            Client.AudioInputDevices.VolumeAdjustment = value;
        }

        public void AdjustOutputVolume(int value)
        {
            if (value is < (-50) or > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Enter an integer value between -50 and 50");
            }

            Client.AudioOutputDevices.VolumeAdjustment = value;
        }

        public async UniTask RefreshAudioDevicesAsync()
        {
            var inputAsyncResult = Client.AudioInputDevices.BeginRefresh(Client.AudioInputDevices.EndRefresh);
            var outputAsyncResult = Client.AudioInputDevices.BeginRefresh(Client.AudioInputDevices.EndRefresh);
            await UniTask.WaitUntil(() => inputAsyncResult.IsCompleted && outputAsyncResult.IsCompleted);
        }

        public async UniTask SetAudioInputDeviceAsync(IAudioDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            await RefreshAudioDevicesAsync();
            if (!Client.AudioInputDevices.AvailableDevices.Contains(device))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The input device of the name '{device.Name}' is not available");
                }
                return;
            }

            _ = Client.AudioInputDevices.BeginSetActiveDevice(device, Client.AudioInputDevices.EndSetActiveDevice);
            await RefreshAudioDevicesAsync();
        }

        public async UniTask SetAudioOutputDeviceAsync(IAudioDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            await RefreshAudioDevicesAsync();
            if (!Client.AudioOutputDevices.AvailableDevices.Contains(device))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The output device of the name '{device.Name}' is not available");
                }
                return;
            }

            _ = Client.AudioOutputDevices.BeginSetActiveDevice(device, Client.AudioOutputDevices.EndSetActiveDevice);
            await RefreshAudioDevicesAsync();
        }

        public void Update3DPosition
        (
            Vector3 speakerPosition,
            Vector3 listenerPosition,
            Vector3 listenerForwardDirection,
            Vector3 listenerUpDirection
        )
        {
            if (positionalChannelSession == null || positionalChannelSession.AudioState != ConnectionState.Connected)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to update 3D position due to unconnected positional channel");
                }
                return;
            }

            positionalChannelSession.Set3DPosition(speakerPosition, listenerPosition, listenerForwardDirection, listenerUpDirection);
        }

        private static bool CheckManualCredentials(VivoxAppConfig config)
            => config != null &&
                !(string.IsNullOrEmpty(config.ApiEndPoint)
                    || string.IsNullOrEmpty(config.Domain)
                    || string.IsNullOrEmpty(config.Issuer)
                    || string.IsNullOrEmpty(config.TokenKey));

        private void OnMessageLogReceived(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
        {
            var channelTextMessage = textMessage.Value;
            var accountName = channelTextMessage.Sender.Name;
            var channelName = channelTextMessage.ChannelSession.Channel.Name;
            var message = channelTextMessage.Message;

            if (Logger.IsDebug())
            {
                Logger.LogDebug("The message is received\n"
                                + $"accountName: {accountName}, channelName: {channelName}, message: {message}");
            }

            onTextMessageReceived.OnNext(new VivoxReceivedValue<string>(accountName, channelName, message));
        }

        private void OnChannelSessionAddedEventHandler(object sender, KeyEventArg<ChannelId> keyEventArg)
        {
            var channelId = keyEventArg.Key;
            var channelName = keyEventArg.Key.Name;

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"ChannelSession with the channel name '{channelName}' was added");
            }

            if (channelId.Type == ChannelType.Positional)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Positional channel was added");
                }

                positionalChannelSession = ActiveChannelSessions[channelId];
            }

            onChannelSessionAdded.OnNext(channelId);
        }

        private void OnChannelSessionRemovedEventHandler(object sender, KeyEventArg<ChannelId> keyEventArg)
        {
            var channelId = keyEventArg.Key;
            var channelName = keyEventArg.Key.Name;

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"ChannelSession with the channel name '{channelName}' was removed");
            }

            var channelSession = (sender as IReadOnlyDictionary<ChannelId, IChannelSession>)[channelId];
            channelSession.PropertyChanged -= OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
            channelSession.MessageLog.AfterItemAdded -= OnMessageLogReceived;

            if (positionalChannelSession?.Channel == channelId)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Positional channel was removed");
                }

                positionalChannelSession = null;
            }

            onChannelSessionRemoved.OnNext(channelId);
        }

        private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "RecoveryState")
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"RecoveryState was changed to {LoginSession.RecoveryState}");
                }

                onRecoveryStateChanged.OnNext(LoginSession.RecoveryState);
            }
            if (propertyChangedEventArgs.PropertyName != "State")
            {
                return;
            }

            if (LoginSession.State == LoginState.LoggingIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client is logging in");
                }
            }
            else if (LoginSession.State == LoginState.LoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client logged in");
                }

                onLoggedIn.OnNext(Unit.Default);
            }
            else if (LoginSession.State == LoginState.LoggingOut)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client is logging out");
                }
            }
            else if (LoginSession.State == LoginState.LoggedOut)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client logged out");
                }

                LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                LoginSession.ChannelSessions.AfterKeyAdded -= OnChannelSessionAddedEventHandler;
                LoginSession.ChannelSessions.BeforeKeyRemoved -= OnChannelSessionRemovedEventHandler;
                LoginSession = null;

                onLoggedOut.OnNext(Unit.Default);
            }
        }

        private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
        {
            var source = sender as IReadOnlyDictionary<string, IParticipant>;
            var participant = source[keyEventArg.Key];
            var channelName = participant.ParentChannelSession.Channel.Name;

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"A user connected to the channel '{channelName}'");
            }

            onUserConnected.OnNext(participant);
        }

        private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
        {
            var source = sender as IReadOnlyDictionary<string, IParticipant>;
            var participant = source[keyEventArg.Key];
            var channelName = participant.ParentChannelSession.Channel.Name;

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"A user disconnected from the channel '{channelName}'");
            }

            onUserDisconnected.OnNext(participant);
        }

        private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
        {
            var participant = valueEventArg.Value;

            var accountName = participant.Account.Name;
            var channelName = participant.ParentChannelSession.Key.Name;
            var property = valueEventArg.PropertyName;

            if (property == "AudioEnergy")
            {
                var audioEnergy = valueEventArg.Value.AudioEnergy;
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("AudioEnergy was changed\n"
                                    + $"accountName: {accountName}, channelName: {channelName}, audioEnergy: {audioEnergy}");
                }

                onAudioEnergyChanged.OnNext(new VivoxReceivedValue<double>(accountName, channelName, audioEnergy));
            }
        }

        private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var channelSession = sender as IChannelSession;

            if (propertyChangedEventArgs.PropertyName == "AudioState" && channelSession.AudioState == ConnectionState.Disconnected)
            {
                var channelName = channelSession.Channel.Name;
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The audio disconnected from channel '{channelName}'");
                }

                foreach (var participant in channelSession.Participants)
                {
                    var accountName = participant.Account.Name;
                    onAudioEnergyChanged.OnNext(new VivoxReceivedValue<double>(accountName, channelName, 0));
                }
            }

            if ((propertyChangedEventArgs.PropertyName is "AudioState" or "TextState")
                && channelSession.AudioState == ConnectionState.Disconnected
                && channelSession.TextState == ConnectionState.Disconnected)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The audio and text disconnected from channel '{channelSession.Channel.Name}'");
                }

                LoginSession.DeleteChannelSession(channelSession.Channel);
            }
        }
    }
}
