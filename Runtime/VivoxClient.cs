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
        public IObservable<Unit> OnLoggedIn => onLoggedIn;
        private readonly Subject<Unit> onLoggedIn = new Subject<Unit>();

        public IObservable<Unit> OnLoggedOut => onLoggedOut;
        private readonly Subject<Unit> onLoggedOut = new Subject<Unit>();

        public IObservable<ConnectionRecoveryState> OnRecoveryStateChanged => onRecoveryStateChanged;
        private readonly Subject<ConnectionRecoveryState> onRecoveryStateChanged
            = new Subject<ConnectionRecoveryState>();

        public IObservable<ChannelId> OnChannelSessionAdded => onChannelSessionAdded;
        private readonly Subject<ChannelId> onChannelSessionAdded = new Subject<ChannelId>();

        public IObservable<ChannelId> OnChannelSessionRemoved => onChannelSessionRemoved;
        private readonly Subject<ChannelId> onChannelSessionRemoved = new Subject<ChannelId>();

        public IObservable<IParticipant> OnUserConnected => onUserConnected;
        private readonly Subject<IParticipant> onUserConnected = new Subject<IParticipant>();

        public IObservable<IParticipant> OnUserDisconnected => onUserDisconnected;
        private readonly Subject<IParticipant> onUserDisconnected = new Subject<IParticipant>();

        public IObservable<VivoxReceivedValue<string>> OnTextMessageReceived => onTextMessageReceived;
        private readonly Subject<VivoxReceivedValue<string>> onTextMessageReceived
            = new Subject<VivoxReceivedValue<string>>();

        public IObservable<VivoxReceivedValue<double>> OnAudioEnergyChanged => onAudioEnergyChanged;
        private readonly Subject<VivoxReceivedValue<double>> onAudioEnergyChanged
            = new Subject<VivoxReceivedValue<double>>();


        public string[] AvailableInputDevices
            => client.AudioInputDevices?.AvailableDevices.Select(device => device.Name).ToArray();
        public string ActiveInputDevice => client.AudioInputDevices?.ActiveDevice.Name;
        public string[] AvailableOutputDevices
            => client.AudioOutputDevices?.AvailableDevices.Select(device => device.Name).ToArray();
        public string ActiveOutputDevice => client.AudioOutputDevices?.ActiveDevice.Name;

        private Client client;
        private ILoginSession loginSession;
        private LoginState LoginState => loginSession != null ? loginSession.State : LoginState.LoggedOut;

        private IChannelSession positionalChannelSession;
        private IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannelSessions
            => loginSession?.ChannelSessions;

        private VivoxConnectionConfig config;

        private bool initialized;

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(VivoxClient));

        public void Dispose()
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Dispose {nameof(VivoxClient)}");
            }

            onLoggedIn.Dispose();
            onLoggedOut.Dispose();
            onRecoveryStateChanged.Dispose();
            onChannelSessionAdded.Dispose();
            onChannelSessionRemoved.Dispose();
            onUserConnected.Dispose();
            onUserDisconnected.Dispose();
            onTextMessageReceived.Dispose();
            onAudioEnergyChanged.Dispose();

            if (loginSession != null)
            {
                foreach (var channelSession in loginSession.ChannelSessions)
                {
                    channelSession.PropertyChanged -= OnChannelPropertyChanged;
                    channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
                    channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
                    channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
                    channelSession.MessageLog.AfterItemAdded -= OnMessageLogReceived;
                }

                loginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                loginSession.ChannelSessions.AfterKeyAdded -= OnChannelSessionAddedEventHandler;
                loginSession.ChannelSessions.BeforeKeyRemoved -= OnChannelSessionRemovedEventHandler;
                loginSession = null;
            }

            Client.Cleanup();
            if (client != null)
            {
                client.Uninitialize();
            }

            GC.SuppressFinalize(this);
        }

        public async UniTask InitializeAsync(VivoxConnectionConfig config)
        {
            if (!CheckManualCredentials(config))
            {
                throw new ArgumentNullException(nameof(config), $"'{nameof(config)}' or some value in it is null");
            }
            if (initialized)
            {
                throw new InvalidOperationException("This client is already initialized");
            }

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Initialize {nameof(VivoxClient)}");
            }

            this.config = config;

            client = new Client(new Uri(config.ApiEndPoint));
            client.Initialize();

            await UniTask.WaitUntil(() => client.Initialized);

            initialized = true;
        }

        public void Login(VivoxLoginParameter loginParameter)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This computer is not connected to the Internet");
                }
                return;
            }
            if (LoginState is LoginState.LoggingIn or LoginState.LoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client already logged into the server");
                }
                return;
            }

            var issuer = config.Issuer;
            var accountName = loginParameter.AccountName;
            var domain = config.Domain;
            var displayName = loginParameter.DisplayName;
            var accountId = new AccountId(issuer, accountName, domain, displayName);
            var tokenSigningKey = config.TokenKey;
            var tokenExpirationDuration = loginParameter.TokenExpirationDuration;

            loginSession = client.GetLoginSession(accountId);
            var loginToken = loginSession.GetLoginToken(tokenSigningKey, TimeSpan.FromSeconds(tokenExpirationDuration));

            loginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            loginSession.ChannelSessions.AfterKeyAdded += OnChannelSessionAddedEventHandler;
            loginSession.ChannelSessions.BeforeKeyRemoved += OnChannelSessionRemovedEventHandler;
            _ = loginSession.BeginLogin(loginToken, SubscriptionMode.Accept, null, null, null, loginSession.EndLogin);
        }

        public void Logout()
        {
            if (LoginState is LoginState.LoggedOut)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client already logged out of the server");
                }
                return;
            }

            loginSession.Logout();
        }

        public void Connect(VivoxConnectionParameter connectionParameter)
        {
            if (LoginState != LoginState.LoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to connect before login");
                }
                return;
            }

            var issuer = config.Issuer;
            var channelName = connectionParameter.ChannelName;
            var domain = config.Domain;
            var channelType = connectionParameter.ChannelType;
            var property = connectionParameter.Properties;
            var tokenSigningKey = config.TokenKey;
            var tokenExpirationDuration = connectionParameter.TokenExpirationDuration;
            var chatCapability = connectionParameter.ChatCapability;
            var transmissionSwitch = connectionParameter.TransmissionSwitch;

            var channel = new ChannelId(issuer, channelName, domain, channelType, property);
            var channelSession = loginSession.GetChannelSession(channel);
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
            _ = channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly, chatCapability != ChatCapability.AudioOnly, transmissionSwitch, connectionToken, channelSession.EndConnect);
        }

        public void Disconnect(ChannelId channelId)
        {
            if (ChannelId.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            if (LoginState != LoginState.LoggedIn || !ActiveChannelSessions.ContainsKey(channelId))
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
            if (LoginState != LoginState.LoggedIn || ActiveChannelSessions.Count == 0)
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

            if (LoginState != LoginState.LoggedIn || !ActiveChannelSessions.ContainsKey(channelId))
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

        public void MuteInputDevice(bool muted)
        {
            if (!initialized)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to mute the input device before initialization");
                }
                return;
            }

            client.AudioInputDevices.Muted = muted;
        }

        public void MuteOutputDevice(bool muted)
        {
            if (!initialized)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to mute the output device before initialization");
                }
                return;
            }

            client.AudioOutputDevices.Muted = muted;
        }

        public void SetTransmissionMode(TransmissionMode mode, ChannelId channelId = default)
        {
            if (LoginState != LoginState.LoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to set the transmission mode before login");
                }
                return;
            }

            if (mode != TransmissionMode.Single)
            {
                loginSession.SetTransmissionMode(mode);
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

            loginSession.SetTransmissionMode(mode, channelId);
        }

        public void AdjustInputVolume(int value)
        {
            if (value is < (-50) or > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Enter an integer value between -50 and 50");
            }

            if (!initialized)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to adjust the input volume before initialization");
                }
                return;
            }

            client.AudioInputDevices.VolumeAdjustment = value;
        }

        public void AdjustOutputVolume(int value)
        {
            if (value is < (-50) or > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Enter an integer value between -50 and 50");
            }

            if (!initialized)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to adjust the output volume before initialization");
                }
                return;
            }

            client.AudioOutputDevices.VolumeAdjustment = value;
        }

        public async UniTask RefreshAudioDevicesAsync()
        {
            if (!initialized)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("Unable to refresh the audio devices before initialization");
                }
                return;
            }

            var inputAsyncResult = client.AudioInputDevices.BeginRefresh(client.AudioInputDevices.EndRefresh);
            var outputAsyncResult = client.AudioInputDevices.BeginRefresh(client.AudioInputDevices.EndRefresh);
            await UniTask.WaitUntil(() => inputAsyncResult.IsCompleted && outputAsyncResult.IsCompleted);
        }

        public async UniTask SetAudioInputDeviceAsync(string name)
        {
            await RefreshAudioDevicesAsync();

            var targetDevice = client.AudioInputDevices.AvailableDevices.FirstOrDefault(device => device.Name == name);
            if (targetDevice == null)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The input device of the name '{name}' is not available");
                }
                return;
            }

            _ = client.AudioInputDevices.BeginSetActiveDevice(targetDevice, client.AudioInputDevices.EndSetActiveDevice);
            await RefreshAudioDevicesAsync();
        }

        public async UniTask SetAudioOutputDeviceAsync(string name)
        {
            await RefreshAudioDevicesAsync();

            var targetDevice = client.AudioOutputDevices.AvailableDevices.FirstOrDefault(device => device.Name == name);
            if (targetDevice == null)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The output device of the name '{name}' is not available");
                }
                return;
            }

            _ = client.AudioOutputDevices.BeginSetActiveDevice(targetDevice, client.AudioOutputDevices.EndSetActiveDevice);
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

        private static bool CheckManualCredentials(VivoxConnectionConfig config)
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
                    Logger.LogDebug($"RecoveryState was changed to {loginSession.RecoveryState}");
                }

                onRecoveryStateChanged.OnNext(loginSession.RecoveryState);
            }
            if (propertyChangedEventArgs.PropertyName != "State")
            {
                return;
            }

            if (LoginState == LoginState.LoggingIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client is logging in");
                }
            }
            else if (LoginState == LoginState.LoggedIn)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client logged in");
                }

                onLoggedIn.OnNext(Unit.Default);
            }
            else if (LoginState == LoginState.LoggingOut)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client is logging out");
                }
            }
            else if (LoginState == LoginState.LoggedOut)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client logged out");
                }

                loginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
                loginSession.ChannelSessions.AfterKeyAdded -= OnChannelSessionAddedEventHandler;
                loginSession.ChannelSessions.BeforeKeyRemoved -= OnChannelSessionRemovedEventHandler;
                loginSession = null;

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

                loginSession.DeleteChannelSession(channelSession.Channel);
            }
        }
    }
}
