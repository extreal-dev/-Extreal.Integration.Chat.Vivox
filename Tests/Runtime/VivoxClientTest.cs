using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VivoxUnity;


namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxClientTest
    {
        private VivoxClient client;

        private bool onLoggedIn;
        private bool onLoggedOut;
        private bool onRecoveryStateChanged;
        private ConnectionRecoveryState changedRecoveryState;

        private bool onChannelSessionAdded;
        private ChannelId addedChannelId;
        private bool onChannelSessionRemoved;
        private ChannelId removedChannelId;

        private bool onUserConnected;
        private IParticipant connectedUser;
        private bool onUserDisconnected;
        private IParticipant disconnectedUser;

        private bool onTextMessageReceived;
        private IChannelTextMessage receivedMessage;
        private bool onAudioEnergyChanged;
        private (IParticipant participant, double audioEnergy) changedAudioEnergy;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [OneTimeSetUp]
        public void OneTimeSetUp()
            => LoggingManager.Initialize(LogLevel.Debug);

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);

            await SceneManager.LoadSceneAsync("Main");

            var chatConfigProvider = UnityEngine.Object.FindObjectOfType<ChatConfigProvider>();
            var chatConfig = chatConfigProvider.ChatConfig;

            client = new VivoxClient(chatConfig.ToVivoxAppConfig());

            onLoggedIn = default;
            onLoggedOut = default;
            onRecoveryStateChanged = default;
            changedRecoveryState = default;

            onChannelSessionAdded = default;
            addedChannelId = default;
            onChannelSessionRemoved = default;
            removedChannelId = default;

            onUserConnected = default;
            connectedUser = default;
            onUserDisconnected = default;
            disconnectedUser = default;

            onTextMessageReceived = default;
            receivedMessage = default;
            onAudioEnergyChanged = default;
            changedAudioEnergy = default;

            _ = client.OnLoggedIn
                .Subscribe(_ => onLoggedIn = true)
                .AddTo(disposables);

            _ = client.OnLoggedOut
                .Subscribe(_ => onLoggedOut = true)
                .AddTo(disposables);

            _ = client.OnRecoveryStateChanged
                .Subscribe(changedRecoveryState =>
                {
                    onRecoveryStateChanged = true;
                    this.changedRecoveryState = changedRecoveryState;
                })
                .AddTo(disposables);

            _ = client.OnChannelSessionAdded
                .Subscribe(addedChannelId =>
                {
                    onChannelSessionAdded = true;
                    this.addedChannelId = addedChannelId;
                })
                .AddTo(disposables);

            _ = client.OnChannelSessionRemoved
                .Subscribe(removedChannelId =>
                {
                    onChannelSessionRemoved = true;
                    this.removedChannelId = removedChannelId;
                })
                .AddTo(disposables);

            _ = client.OnUserConnected
                .Subscribe(connectedUser =>
                {
                    onUserConnected = true;
                    this.connectedUser = connectedUser;
                })
                .AddTo(disposables);

            _ = client.OnUserDisconnected
                .Subscribe(disconnectedUser =>
                {
                    onUserDisconnected = true;
                    this.disconnectedUser = disconnectedUser;
                })
                .AddTo(disposables);

            _ = client.OnTextMessageReceived
                .Subscribe(receivedMessage =>
                {
                    onTextMessageReceived = true;
                    this.receivedMessage = receivedMessage;
                })
                .AddTo(disposables);

            _ = client.OnAudioEnergyChanged
                .Subscribe(changedAudioEnergy =>
                {
                    onAudioEnergyChanged = true;
                    this.changedAudioEnergy = changedAudioEnergy;
                })
                .AddTo(disposables);
        });

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            if (client.LoginSession?.State == LoginState.LoggedIn)
            {
                client.Logout();
                await UniTask.WaitUntil(() => onLoggedOut);
            }

            client.Dispose();
            disposables.Clear();
        });

        [OneTimeTearDown]
        public void OneTimeTearDown()
            => disposables.Dispose();

        [Test]
        public void NewVivoxClientWithConfigNull()
            => Assert.That(() => _ = new VivoxClient(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("appConfig"));

        [UnityTest]
        public IEnumerator LoginSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);
            Assert.IsTrue(onRecoveryStateChanged);
            Assert.AreEqual(ConnectionRecoveryState.Connected, changedRecoveryState);
        });

        [UnityTest]
        public IEnumerator DisposeWithoutDisconnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.Dispose();
            onLoggedOut = true;
        });

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnection() => UniTask.ToCoroutine(async () =>
        {
            Debug.Log("<color=lime>Disable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability == NetworkReachability.NotReachable);

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            static void ErrorHandling(string logText, string traceBack, LogType logType)
            {
                if (logType == LogType.Error)
                {
                    LogAssert.Expect(LogType.Error, logText);
                }
            }

            Exception exception = null;

            Application.logMessageReceived += ErrorHandling;
            try
            {
                await client.LoginAsync(authConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Application.logMessageReceived -= ErrorHandling;

            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The login timed-out", exception.Message);

            Debug.Log("<color=lime>Enable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);
            await UniTask.Delay(TimeSpan.FromSeconds(10));
        });

        [UnityTest]
        public IEnumerator LoginTwice() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await client.LoginAsync(authConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client already logging/logged into the server");
        });

        [UnityTest]
        public IEnumerator LogoutSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);
        });

        [Test]
        public void LogoutWithoutLogin()
        {
            client.Logout();
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has never logged into the server before");
        }

        [UnityTest]
        public IEnumerator ConnectSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onChannelSessionAdded);
            Assert.IsTrue(onUserConnected);
            Assert.AreEqual(displayName, connectedUser.Account.DisplayName);
            Assert.AreEqual(authConfig.AccountName, connectedUser.Account.Name);
            Assert.AreEqual(channelName, addedChannelId.Name);
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnection() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            Debug.Log("<color=lime>Disable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability == NetworkReachability.NotReachable);
            Debug.Log("<color=lime>Enable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);
            Debug.Log("<color=lime>Disable the Internet connection</color>");

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName, timeout: TimeSpan.FromSeconds(35));

            Exception exception = null;
            try
            {
                await client.ConnectAsync(channelConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(TimeoutException), exception.GetType());
            Assert.AreEqual("The connection timed-out", exception.Message);

            Debug.Log("<color=lime>Enable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);
            await UniTask.Delay(TimeSpan.FromSeconds(10));
        });

        [UnityTest]
        public IEnumerator ConnectWithoutLogin() => UniTask.ToCoroutine(async () =>
        {
            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to connect before login");
        });

        [UnityTest]
        public IEnumerator ConnectTwice() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            await client.ConnectAsync(channelConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client already connected to the channel '{channelName}'");
        });

        [UnityTest]
        public IEnumerator DisconnectSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.Disconnect(addedChannelId);
            await UniTask.WaitUntil(() => onChannelSessionRemoved);
            await UniTask.WaitUntil(() => onUserDisconnected);
            Assert.AreEqual(channelName, removedChannelId.Name);
            Assert.IsTrue(onUserDisconnected);
            Assert.IsTrue(disconnectedUser.IsSelf);
        });

        [UnityTest]
        public IEnumerator DisconnectWithoutLogin() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

            client.Disconnect(addedChannelId);
            Assert.IsFalse(onUserDisconnected);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has already disconnected from the channel");
        });

        [Test]
        public void DisconnectWithAccountIdNull()
            => Assert.That(() => client.Disconnect(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelId"));

        [UnityTest]
        public IEnumerator DisconnectAllChannelsSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.DisconnectAllChannels();
            await UniTask.WaitUntil(() => onChannelSessionRemoved);
            Assert.AreEqual(channelName, removedChannelId.Name);
        });

        [Test]
        public void DisconnectAllChannelsWithoutLogin()
        {
            client.DisconnectAllChannels();
            Assert.IsFalse(onUserDisconnected);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has already disconnected from all channels");
        }

        [UnityTest]
        public IEnumerator DisconnectAllChannelsWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            client.DisconnectAllChannels();
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has already disconnected from all channels");
        });

        [UnityTest]
        public IEnumerator UserConnected() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);
            onUserConnected = false;

            Debug.Log("<color=lime>Run 'UserConnectedSub' test in another Unity Editor window</color>");
            await UniTask.WaitUntil(() => onUserConnected);
            await UniTask.WaitUntil(() => onUserDisconnected);
        });

        [UnityTest]
        public IEnumerator SendMessageSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            var addedChannelSession = client.LoginSession.GetChannelSession(addedChannelId);
            await UniTask.WaitUntil(() => addedChannelSession.TextState == ConnectionState.Connected);

            const string message = "This is a test message";
            client.SendTextMessage(message, new ChannelId[] { addedChannelId });
            await UniTask.WaitUntil(() => onTextMessageReceived);
            Assert.AreEqual(authConfig.AccountName, receivedMessage.Sender.Name);
            Assert.AreEqual(channelName, receivedMessage.ChannelSession.Channel.Name);
            Assert.AreEqual(message, receivedMessage.Message);
        });

        [Test]
        public void SendMessageWithMessageNull()
            => Assert.That(() => client.SendTextMessage(null, new ChannelId("issuer", "TestUser", "domain")),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("message"));

        [Test]
        public void SendMessageWithAccountIdNull()
            => Assert.That(() => client.SendTextMessage("This is a test message", null as ChannelId),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelId"));

        [Test]
        public void SendMessageWithChannelIdsNull()
            => Assert.That(() => client.SendTextMessage("This is a test message", null as ChannelId[]),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelIds"));

        [Test]
        public void SendMessageWithoutLogin()
        {
            const string channelName = "TestChannel";
            const string message = "This is a test message";
            client.SendTextMessage(message, new ChannelId("issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to send a message before connecting to the channel '{channelName}'");
        }

        [UnityTest]
        public IEnumerator SendMessageWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            const string message = "This is a test message";
            client.SendTextMessage(message, new ChannelId("issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to send a message before connecting to the channel '{channelName}'");
        });

        [UnityTest]
        public IEnumerator SetTransmissionModeToAll() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            client.SetTransmissionMode(TransmissionMode.All);
        });

        [UnityTest]
        public IEnumerator SetTransmissionModeToSingle() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.SetTransmissionMode(TransmissionMode.Single, addedChannelId);
        });

        [Test]
        public void SetTransmissionModeWithoutLogin()
        {
            client.SetTransmissionMode(TransmissionMode.All);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to set the transmission mode before login");
        }

        [UnityTest]
        public IEnumerator SetTransmissionModeToSingleWithChannelNameNull() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            Assert.That(() => client.SetTransmissionMode(TransmissionMode.Single),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("Expects the ID of the channel to be set in single transmission mode"));
        });

        [Test]
        public void SetTransmissionModeToSingleWithoutLogin()
        {
            const string channelName = "TestChannel";
            client.SetTransmissionMode(TransmissionMode.Single, new ChannelId("issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to set the transmission mode before login");
        }

        [UnityTest]
        public IEnumerator SetTransmissionModeToSingleWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            client.SetTransmissionMode(TransmissionMode.Single, new ChannelId("issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to set transmission mode to 'Single' before connecting to the channel '{channelName}'");
        });

        [UnityTest]
        public IEnumerator SetAudioInputDeviceSuccess() => UniTask.ToCoroutine(async () =>
        {
            var preActiveInputDevice = (await client.GetAudioInputDevicesAsync()).ActiveDevice;
            var targetInputDevice = (await client.GetAudioInputDevicesAsync()).AvailableDevices
                                        .First(device => device != preActiveInputDevice);
            Assert.AreNotEqual(preActiveInputDevice, targetInputDevice);

            await client.SetActiveAudioInputDeviceAsync(targetInputDevice);
            Assert.AreEqual(targetInputDevice, (await client.GetAudioInputDevicesAsync()).ActiveDevice);
        });

        [UnityTest]
        public IEnumerator SetAudioInputDeviceWithDeviceNull() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await client.SetActiveAudioInputDeviceAsync(null);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(ArgumentNullException), exception.GetType());
            Assert.IsTrue(exception.Message.Contains("device"));
        });

        [UnityTest]
        public IEnumerator SetAudioInputDeviceWithInvalidDevice() => UniTask.ToCoroutine(async () =>
        {
            var availableInputDevices = (await client.GetAudioInputDevicesAsync()).AvailableDevices;
            var availableOutputDevices = (await client.GetAudioOutputDevicesAsync()).AvailableDevices;
            var invalidDevice = availableOutputDevices.Except(availableInputDevices).First();
            await client.SetActiveAudioInputDeviceAsync(invalidDevice);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] The input device of the name '{invalidDevice.Name}' is not available");
        });

        [UnityTest]
        public IEnumerator SetAudioOutputDeviceSuccess() => UniTask.ToCoroutine(async () =>
        {
            var preActiveOutputDevice = (await client.GetAudioOutputDevicesAsync()).ActiveDevice;
            var targetOutputDevice = (await client.GetAudioOutputDevicesAsync()).AvailableDevices
                                        .First(device => device != preActiveOutputDevice);
            Assert.AreNotEqual(preActiveOutputDevice, targetOutputDevice);

            await client.SetActiveAudioOutputDeviceAsync(targetOutputDevice);
            Assert.AreEqual(targetOutputDevice, (await client.GetAudioOutputDevicesAsync()).ActiveDevice);
        });

        [UnityTest]
        public IEnumerator SetAudioOutputDeviceWithDeviceNull() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await client.SetActiveAudioOutputDeviceAsync(null);
            }
            catch (Exception e)
            {
                exception = e;
            }
            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(ArgumentNullException), exception.GetType());
            Assert.IsTrue(exception.Message.Contains("device"));
        });

        [UnityTest]
        public IEnumerator SetAudioOutputDeviceWithInvalidDevice() => UniTask.ToCoroutine(async () =>
        {
            var availableInputDevices = (await client.GetAudioInputDevicesAsync()).AvailableDevices;
            var availableOutputDevices = (await client.GetAudioOutputDevicesAsync()).AvailableDevices;
            var invalidDevice = availableInputDevices.Except(availableOutputDevices).First();
            await client.SetActiveAudioOutputDeviceAsync(invalidDevice);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] The output device of the name '{invalidDevice.Name}' is not available");
        });

        [UnityTest]
        public IEnumerator AudioEnergyChanged() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            Debug.Log("<color=lime>Make a sound</color>");
            await UniTask.WaitUntil(() => onAudioEnergyChanged);
            Assert.AreNotEqual(0, changedAudioEnergy.audioEnergy);
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            Debug.Log("<color=lime>Disable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability == NetworkReachability.NotReachable);

            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.Recovering);
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);
            await UniTask.WaitUntil(() => onLoggedOut);

            Debug.Log("<color=lime>Enable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);
            await UniTask.Delay(TimeSpan.FromSeconds(10));
        });
    }
}
