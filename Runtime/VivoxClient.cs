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
    /// <summary>
    /// Class that handles a client for Vivox.
    /// </summary>
    public class VivoxClient : IDisposable
    {
#pragma warning disable CC0033
        /// <summary>
        /// Invokes immediately after logging into the server.
        /// </summary>
        public IObservable<Unit> OnLoggedIn => onLoggedIn.AddTo(disposables);
        private readonly Subject<Unit> onLoggedIn = new Subject<Unit>();

        /// <summary>
        /// Invokes immediately after logging out of the server.
        /// </summary>
        public IObservable<Unit> OnLoggedOut => onLoggedOut.AddTo(disposables);
        private readonly Subject<Unit> onLoggedOut = new Subject<Unit>();

        /// <summary>
        /// <para>Invokes immediately after the recovery state is changed.</para>
        /// Arg: Changed recovery state
        /// </summary>
        public IObservable<ConnectionRecoveryState> OnRecoveryStateChanged
            => onRecoveryStateChanged.AddTo(disposables);
        private readonly Subject<ConnectionRecoveryState> onRecoveryStateChanged
            = new Subject<ConnectionRecoveryState>();

        /// <summary>
        /// <para>Invokes immediately after the channel session is added.</para>
        /// Arg: ID of the added channel
        /// </summary>
        public IObservable<ChannelId> OnChannelSessionAdded => onChannelSessionAdded.AddTo(disposables);
        private readonly Subject<ChannelId> onChannelSessionAdded = new Subject<ChannelId>();

        /// <summary>
        /// <para>Invokes immediately after the channel session is removed.</para>
        /// Arg: ID of the removed channel
        /// </summary>
        public IObservable<ChannelId> OnChannelSessionRemoved => onChannelSessionRemoved.AddTo(disposables);
        private readonly Subject<ChannelId> onChannelSessionRemoved = new Subject<ChannelId>();

        /// <summary>
        /// <para>Invokes immediately after a user connects to the channel session.</para>
        /// Arg: Information of the connected user
        /// </summary>
        public IObservable<IParticipant> OnUserConnected => onUserConnected.AddTo(disposables);
        private readonly Subject<IParticipant> onUserConnected = new Subject<IParticipant>();

        /// <summary>
        /// <para>Invokes immediately after a user disconnects from the channel session.</para>
        /// Arg: Information of the disconnected user
        /// </summary>
        public IObservable<IParticipant> OnUserDisconnected => onUserDisconnected.AddTo(disposables);
        private readonly Subject<IParticipant> onUserDisconnected = new Subject<IParticipant>();

        /// <summary>
        /// <para>Invokes immediately after a text message is received.</para>
        /// Arg: Received text message and information of the sender
        /// </summary>
        public IObservable<IChannelTextMessage> OnTextMessageReceived => onTextMessageReceived.AddTo(disposables);
        private readonly Subject<IChannelTextMessage> onTextMessageReceived
            = new Subject<IChannelTextMessage>();

        /// <summary>
        /// <para>Invokes immediately after the audio energy of any user is changed.</para>
        /// Arg: Changed audio energy and information of the user
        /// </summary>
        public IObservable<(IParticipant participant, double audioEnergy)> OnAudioEnergyChanged
            => onAudioEnergyChanged.AddTo(disposables);
        private readonly Subject<(IParticipant participant, double audioEnergy)> onAudioEnergyChanged
            = new Subject<(IParticipant participant, double audioEnergy)>();
#pragma warning restore CC0033

        /// <summary>
        /// Created client for Vivox.
        /// </summary>
        /// <value>Created client for Vivox.</value>
        public Client Client { get; private set; }

        /// <summary>
        /// Created login session for this client.
        /// </summary>
        /// <value>Created login session for this client.</value>
        public ILoginSession LoginSession { get; private set; }

        private bool IsLoggingIn => LoginSession?.State == LoginState.LoggingIn;
        private bool IsLoggedIn => LoginSession?.State == LoginState.LoggedIn;

        private IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannelSessions
            => LoginSession?.ChannelSessions;

        private readonly VivoxAppConfig appConfig;
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        private static readonly ELogger Logger = LoggingManager.GetLogger(nameof(VivoxClient));

        /// <summary>
        /// Creates a new VivoxClient with given appConfig.
        /// </summary>
        /// <param name="appConfig">Application config to create a client.</param>
        /// <exception cref="ArgumentNullException">If 'appConfig' or some value in it is null</exception>
        public VivoxClient(VivoxAppConfig appConfig)
        {
            CheckManualCredentials(appConfig);

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"Initialize {nameof(VivoxClient)}");
            }

            this.appConfig = appConfig;

            Client = new Client(new Uri(appConfig.ApiEndPoint));
            Client.Initialize();
        }

        /// <summary>
        /// Disposes VivoxClient
        /// </summary>
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
                    RemoveChannelSessionEventHandler(channelSession);
                }

                RemoveLoginSessionEventHandler();
                LoginSession = null;
            }

            Client.Cleanup();
            Client?.Uninitialize();

            GC.SuppressFinalize(this);

        }

        /// <summary>
        /// Logs into the server.
        /// </summary>
        /// <param name="authConfig">Authentication config for login.</param>
        public void Login(VivoxAuthConfig authConfig)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This device is not connected to the Internet");
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

            var accountId = new AccountId
            (
                appConfig.Issuer,
                authConfig.AccountName,
                appConfig.Domain,
                authConfig.DisplayName
            );
            LoginSession = Client.GetLoginSession(accountId);
            var loginToken = LoginSession.GetLoginToken(appConfig.SecretKey, authConfig.TokenExpirationDuration);

            AddLoginSessionEventHandler();
            _ = LoginSession.BeginLogin(loginToken, SubscriptionMode.Accept, null, null, null, LoginSession.EndLogin);
        }

        /// <summary>
        /// Logs out of the server.
        /// </summary>
        public void Logout()
        {
            if (LoginSession == null)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("This client has never logged into the server before");
                }
                return;
            }

            LoginSession.Logout();
        }

        /// <summary>
        /// Connects to the channel.
        /// </summary>
        /// <param name="channelConfig">Channel config for connection.</param>
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

            var channel = new ChannelId
            (
                appConfig.Issuer,
                channelConfig.ChannelName,
                appConfig.Domain,
                channelConfig.ChannelType,
                channelConfig.Properties
            );

            var channelSession = LoginSession.GetChannelSession(channel);
            if (channelSession.ChannelState != ConnectionState.Disconnected)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"This client already connected to the channel '{channelConfig.ChannelName}'");
                }
                return;
            }

            AddChannelSessionEventHandler(channelSession);

            var connectionToken = channelSession.GetConnectToken(appConfig.SecretKey, channelConfig.TokenExpirationDuration);
            _ = channelSession.BeginConnect
            (
                channelConfig.ChatType != ChatType.TextOnly,
                channelConfig.ChatType != ChatType.AudioOnly,
                channelConfig.TransmissionSwitch,
                connectionToken,
                channelSession.EndConnect
            );
        }

        /// <summary>
        /// Disconnects from the channel.
        /// </summary>
        /// <param name="channelId">ID of the channel to be disconnected.</param>
        public void Disconnect(ChannelId channelId)
        {
            if (ChannelId.IsNullOrEmpty(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            LoginSession.DeleteChannelSession(channelId);
        }

        /// <summary>
        /// Disconnects from all channels.
        /// </summary>
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
                LoginSession.DeleteChannelSession(activeChannelId);
            }
        }

        /// <summary>
        /// Sends a text message to the several channels.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <param name="channelIds">IDs of the channels to which the message is sent.</param>
        /// <param name="language">Language of the message.</param>
        /// <param name="applicationStanzaNamespace">Optional namespace element for additional application data.</param>
        /// <param name="applicationStanzaBody">Additional application data body.</param>
        /// <exception cref="ArgumentNullException">If 'message', 'channelIds' or some channel ID in 'channelIds' is null.</exception>
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
                SendTextMessage(message, channelId, language, applicationStanzaNamespace, applicationStanzaBody);
            }
        }

        /// <summary>
        /// Sends a text message to the channel.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        /// <param name="channelId">ID of the channel to which the message is sent.</param>
        /// <param name="language">Language of the message.</param>
        /// <param name="applicationStanzaNamespace">Optional namespace element for additional application data.</param>
        /// <param name="applicationStanzaBody">Additional application data body.</param>
        /// <exception cref="ArgumentNullException">If 'message' or 'channelId' is null.</exception>
        public void SendTextMessage
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
                return;
            }

            var channelSession = ActiveChannelSessions[channelId];
            _ = channelSession.BeginSendText(language, message, applicationStanzaNamespace, applicationStanzaBody, channelSession.EndSendText);
        }

        /// <summary>
        /// Sets the transmission mode.
        /// </summary>
        /// <param name="mode">Transmission mode to be set.</param>
        /// <param name="channelId">ID of the channel to be transmitted when 'mode' is 'Single'.</param>
        /// <exception cref="ArgumentNullException">If 'channelId' is null when 'mode' is 'Single'.</exception>
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

        /// <summary>
        /// Gets the active audio input device.
        /// </summary>
        /// <returns>Active audio input device.</returns>
        public async UniTask<IAudioDevice> GetActiveAudioInputDeviceAsync()
        {
            await RefreshAudioInputDevicesAsync();
            return Client.AudioInputDevices.ActiveDevice;
        }

        /// <summary>
        /// Gets the active audio output device.
        /// </summary>
        /// <returns>Active audio output device.</returns>
        public async UniTask<IAudioDevice> GetActiveAudioOutputDeviceAsync()
        {
            await RefreshAudioOutputDevicesAsync();
            return Client.AudioOutputDevices.ActiveDevice;
        }

        /// <summary>
        /// Gets the available audio input devices.
        /// </summary>
        /// <returns>Available audio input devices.</returns>
        public async UniTask<IReadOnlyDictionary<string, IAudioDevice>> GetAvailableAudioInputDevicesAsync()
        {
            await RefreshAudioInputDevicesAsync();
            return Client.AudioInputDevices.AvailableDevices;
        }

        /// <summary>
        /// Gets the available audio output devices.
        /// </summary>
        /// <returns>Available audio output devices.</returns>
        public async UniTask<IReadOnlyDictionary<string, IAudioDevice>> GetAvailableAudioOutputDevicesAsync()
        {
            await RefreshAudioOutputDevicesAsync();
            return Client.AudioOutputDevices.AvailableDevices;
        }

        /// <summary>
        /// Sets the audio input device.
        /// </summary>
        /// <param name="device">Audio input device to be set.</param>
        /// <exception cref="ArgumentNullException">If 'device' is null.</exception>
        /// <returns>UniTask of this method.</returns>
        public async UniTask SetAudioInputDeviceAsync(IAudioDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            await RefreshAudioInputDevicesAsync();
            if (!Client.AudioInputDevices.AvailableDevices.Contains(device))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The input device of the name '{device.Name}' is not available");
                }
                return;
            }

            _ = Client.AudioInputDevices.BeginSetActiveDevice(device, Client.AudioInputDevices.EndSetActiveDevice);
        }

        /// <summary>
        /// Sets the audio output device.
        /// </summary>
        /// <param name="device">Audio output device to be set.</param>
        /// <exception cref="ArgumentNullException">If 'device' is null.</exception>
        /// <returns>UniTask of this method.</returns>
        public async UniTask SetAudioOutputDeviceAsync(IAudioDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            await RefreshAudioOutputDevicesAsync();
            if (!Client.AudioOutputDevices.AvailableDevices.Contains(device))
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The output device of the name '{device.Name}' is not available");
                }
                return;
            }

            _ = Client.AudioOutputDevices.BeginSetActiveDevice(device, Client.AudioOutputDevices.EndSetActiveDevice);
        }

        private async UniTask RefreshAudioInputDevicesAsync()
        {
            var inputAsyncResult = Client.AudioInputDevices.BeginRefresh(Client.AudioInputDevices.EndRefresh);
            await UniTask.WaitUntil(() => inputAsyncResult.IsCompleted);
        }

        private async UniTask RefreshAudioOutputDevicesAsync()
        {
            var outputAsyncResult = Client.AudioOutputDevices.BeginRefresh(Client.AudioOutputDevices.EndRefresh);
            await UniTask.WaitUntil(() => outputAsyncResult.IsCompleted);
        }

        private static void CheckManualCredentials(VivoxAppConfig appConfig)
        {
            if (appConfig == null
                    || string.IsNullOrEmpty(appConfig.ApiEndPoint)
                    || string.IsNullOrEmpty(appConfig.Domain)
                    || string.IsNullOrEmpty(appConfig.Issuer)
                    || string.IsNullOrEmpty(appConfig.SecretKey))
            {
                throw new ArgumentNullException(nameof(appConfig), $"'{nameof(appConfig)}' or some value in it is null");
            }
        }

        private void OnMessageLogReceived(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
        {
            if (Logger.IsDebug())
            {
                Logger.LogDebug("The message is received");
            }

            onTextMessageReceived.OnNext(textMessage.Value);
        }

        private void OnChannelSessionAddedEventHandler(object sender, KeyEventArg<ChannelId> keyEventArg)
        {
            var channelId = keyEventArg.Key;
            var channelName = keyEventArg.Key.Name;

            if (Logger.IsDebug())
            {
                Logger.LogDebug($"ChannelSession with the channel name '{channelName}' was added");
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
            RemoveChannelSessionEventHandler(channelSession);

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

                RemoveLoginSessionEventHandler();
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
            var property = valueEventArg.PropertyName;

            if (property == "AudioEnergy")
            {
                var audioEnergy = valueEventArg.Value.AudioEnergy;
                if (Logger.IsDebug())
                {
                    Logger.LogDebug("AudioEnergy was changed");
                }

                onAudioEnergyChanged.OnNext((participant, audioEnergy));
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
                    onAudioEnergyChanged.OnNext((participant, 0));
                }
            }

            if (propertyChangedEventArgs.PropertyName is "AudioState" or "TextState"
                && channelSession.AudioState == ConnectionState.Disconnected
                && channelSession.TextState == ConnectionState.Disconnected)
            {
                if (Logger.IsDebug())
                {
                    Logger.LogDebug($"The audio and text disconnected from channel '{channelSession.Channel.Name}'");
                }

                if (LoginSession.RecoveryState == ConnectionRecoveryState.FailedToRecover)
                {
                    LoginSession.DeleteChannelSession(channelSession.Channel);
                }
            }
        }

        private void AddChannelSessionEventHandler(IChannelSession channelSession)
        {
            channelSession.PropertyChanged += OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
            channelSession.MessageLog.AfterItemAdded += OnMessageLogReceived;
        }

        private void RemoveChannelSessionEventHandler(IChannelSession channelSession)
        {
            channelSession.PropertyChanged -= OnChannelPropertyChanged;
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
            channelSession.MessageLog.AfterItemAdded -= OnMessageLogReceived;
        }

        private void AddLoginSessionEventHandler()
        {
            LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            LoginSession.ChannelSessions.AfterKeyAdded += OnChannelSessionAddedEventHandler;
            LoginSession.ChannelSessions.BeforeKeyRemoved += OnChannelSessionRemovedEventHandler;
        }

        private void RemoveLoginSessionEventHandler()
        {
            LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            LoginSession.ChannelSessions.AfterKeyAdded -= OnChannelSessionAddedEventHandler;
            LoginSession.ChannelSessions.BeforeKeyRemoved -= OnChannelSessionRemovedEventHandler;
        }
    }
}
