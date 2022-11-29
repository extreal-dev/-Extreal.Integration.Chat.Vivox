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
        private VivoxReceivedValue<string> receivedMessage;
        private bool onAudioEnergyChanged;
        private VivoxReceivedValue<double> changedAudioEnergy;

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

            var vivoxAppConfigProvider = UnityEngine.Object.FindObjectOfType<VivoxAppConfigProvider>();
            var vivoxAppConfig = vivoxAppConfigProvider.VivoxAppConfig;

            client = new VivoxClient(vivoxAppConfig);

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
            if (onLoggedIn)
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
                    .With.Message.Contain("'config' or some value in it is null"));

        [UnityTest]
        public IEnumerator LoginSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);
            Assert.IsTrue(onRecoveryStateChanged);
            Assert.AreEqual(ConnectionRecoveryState.Connected, changedRecoveryState);
        });

        [UnityTest]
        public IEnumerator DisposeWithoutDisconnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            client.Dispose();
            onLoggedOut = true;
        });

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnection() => UniTask.ToCoroutine(async () =>
        {
            await UniTask.WaitUntil(() => Application.internetReachability == NetworkReachability.NotReachable);

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This computer is not connected to the Internet");

            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);
            await UniTask.Delay(TimeSpan.FromSeconds(10));
        });

        [UnityTest]
        public IEnumerator LoginTwice() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            client.Login(authConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client already logging/logged into the server");
        });

        [UnityTest]
        public IEnumerator LogoutSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);
        });

        [Test]
        public void LogoutWithoutLogin()
        {
            client.Logout();
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client already logged out of the server");
        }

        [UnityTest]
        public IEnumerator ConnectSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);
            Assert.IsTrue(onChannelSessionAdded);
            Assert.AreEqual(displayName, connectedUser.Account.DisplayName);
            Assert.AreEqual(authConfig.AccountName, connectedUser.Account.Name);
            Assert.AreEqual(channelName, addedChannelId.Name);
        });

        [Test]
        public void ConnectWithoutLogin()
        {
            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to connect before login");
        }

        [UnityTest]
        public IEnumerator ConnectTwice() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            client.Connect(channelConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client already connected to the channel '{channelName}'");
        });

        [UnityTest]
        public IEnumerator DisconnectSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            client.Disconnect(addedChannelId);
            await UniTask.WaitUntil(() => onChannelSessionRemoved);
            Assert.AreEqual(channelName, removedChannelId.Name);
            Assert.IsTrue(onUserDisconnected);
            Assert.IsTrue(disconnectedUser.IsSelf);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);
        });

        [Test]
        public void DisconnectWithAccountIdNull()
            => Assert.That(() => client.Disconnect(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelId"));

        [Test]
        public void DisconnectWithoutLogin()
        {
            const string channelName = "TestChannel";
            client.Disconnect(new ChannelId("Issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has not connected to the channel '{channelName}' yet");
        }

        [UnityTest]
        public IEnumerator DisconnectWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            client.Disconnect(new ChannelId("Issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has not connected to the channel '{channelName}' yet");
        });

        [UnityTest]
        public IEnumerator DisconnectAllChannelsSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            client.DisconnectAllChannels();
            await UniTask.WaitUntil(() => onChannelSessionRemoved);
            Assert.AreEqual(channelName, removedChannelId.Name);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);
        });

        [Test]
        public void DisconnectAllChannelsWithoutLogin()
        {
            client.DisconnectAllChannels();
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has already disconnected from all channels");
        }

        [UnityTest]
        public IEnumerator DisconnectAllChannelsWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            client.DisconnectAllChannels();
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client has already disconnected from all channels");
        });

        [UnityTest]
        public IEnumerator SendMessageSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            const string message = "This is a test message";
            var failedSentChannelIds = client.SendTextMessage(message, new ChannelId[] { addedChannelId });
            Assert.IsEmpty(failedSentChannelIds);
            await UniTask.WaitUntil(() => onTextMessageReceived);
            Assert.AreEqual(authConfig.AccountName, receivedMessage.AccountName);
            Assert.AreEqual(channelName, receivedMessage.ChannelName);
            Assert.AreEqual(message, receivedMessage.ReceivedValue);
        });

        [UnityTest]
        public IEnumerator SendMessageFailed() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            var invalidChannelId = new ChannelId("issuer", "TestChannel", "domain");
            const string message = "This is a test message";
            var failedSentChannelIds = client.SendTextMessage(message, new ChannelId[] { invalidChannelId });
            Assert.IsNotEmpty(failedSentChannelIds);
            Assert.AreEqual(invalidChannelId, failedSentChannelIds[0]);
        });

        [Test]
        public void SendMessageWithMessageNull()
            => Assert.That(() => _ = client.SendTextMessage(null, new ChannelId("issuer", "TestUser", "domain")),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("message"));

        [Test]
        public void SendMessageWithAccountIdNull()
            => Assert.That(() => _ = client.SendTextMessage("This is a test message", null as ChannelId),
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
            var result = client.SendTextMessage(message, new ChannelId("issuer", channelName, "domain"));
            Assert.IsFalse(result);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to send a message before connecting to the channel '{channelName}'");
        }

        [UnityTest]
        public IEnumerator SendMessageWithoutConnect() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            const string message = "This is a test message";
            var result = client.SendTextMessage(message, new ChannelId("issuer", channelName, "domain"));
            Assert.IsFalse(result);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to send a message before connecting to the channel '{channelName}'");
        });

        [UnityTest]
        public IEnumerator SetTransmissionModeToAll() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            client.SetTransmissionMode(TransmissionMode.All);
        });

        [UnityTest]
        public IEnumerator SetTransmissionModeToSingle() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

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
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

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
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            client.SetTransmissionMode(TransmissionMode.Single, new ChannelId("issuer", channelName, "domain"));
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to set transmission mode to 'Single' before connecting to the channel '{channelName}'");
        });

        [Test]
        public void AdjustInputVolumeSuccess()
            => client.AdjustInputVolume(0);

        [Test]
        public void AdjustInputVolumeWithOutOfRangeValue()
            => Assert.That(() => client.AdjustInputVolume(100),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contain("Enter an integer value between -50 and 50"));

        [Test]
        public void AdjustOutputVolumeSuccess()
            => client.AdjustOutputVolume(0);

        [Test]
        public void AdjustOutputVolumeWithOutOfRangeValue()
            => Assert.That(() => client.AdjustOutputVolume(100),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contain("Enter an integer value between -50 and 50"));

        [UnityTest]
        public IEnumerator RefreshAudioDevicesSuccess() => UniTask.ToCoroutine(async () =>
            await client.RefreshAudioDevicesAsync()
        );

        [UnityTest]
        public IEnumerator SetAudioInputDeviceSuccess() => UniTask.ToCoroutine(async () =>
        {
            await client.RefreshAudioDevicesAsync();
            var preActiveInputDevice = client.Client.AudioInputDevices.ActiveDevice;
            var targetInputDevice = client.Client.AudioInputDevices.AvailableDevices.First(device => device != preActiveInputDevice);
            Assert.AreNotEqual(preActiveInputDevice, targetInputDevice);

            await client.SetAudioInputDeviceAsync(targetInputDevice);
            Assert.AreEqual(targetInputDevice, client.Client.AudioInputDevices.ActiveDevice);
        });

        [UnityTest]
        public IEnumerator SetAudioInputDeviceWithDeviceNull() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await client.SetAudioInputDeviceAsync(null);
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
            await client.RefreshAudioDevicesAsync();
            var invalidDevice = client.Client.AudioOutputDevices.AvailableDevices.Except(client.Client.AudioInputDevices.AvailableDevices).First();
            await client.SetAudioInputDeviceAsync(invalidDevice);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] The input device of the name '{invalidDevice.Name}' is not available");
        });

        [UnityTest]
        public IEnumerator SetAudioOutputDeviceSuccess() => UniTask.ToCoroutine(async () =>
        {
            await client.RefreshAudioDevicesAsync();
            var preActiveOutputDevice = client.Client.AudioOutputDevices.ActiveDevice;
            var targetOutputDevice = client.Client.AudioOutputDevices.AvailableDevices.First(device => device != preActiveOutputDevice);
            Assert.AreNotEqual(preActiveOutputDevice, targetOutputDevice);

            await client.SetAudioOutputDeviceAsync(targetOutputDevice);
            Assert.AreEqual(targetOutputDevice, client.Client.AudioOutputDevices.ActiveDevice);
        });

        [UnityTest]
        public IEnumerator SetAudioOutputDeviceWithDeviceNull() => UniTask.ToCoroutine(async () =>
        {
            Exception exception = null;
            try
            {
                await client.SetAudioOutputDeviceAsync(null);
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
            await client.RefreshAudioDevicesAsync();
            var invalidDevice = client.Client.AudioInputDevices.AvailableDevices.Except(client.Client.AudioOutputDevices.AvailableDevices).First();
            await client.SetAudioOutputDeviceAsync(invalidDevice);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] The output device of the name '{invalidDevice.Name}' is not available");
        });

        [UnityTest]
        public IEnumerator Update3DPositionSuccess() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName, channelType: ChannelType.Positional);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected && connectedUser.ParentChannelSession.AudioState == ConnectionState.Connected);

            client.Update3DPosition(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
        });

        [UnityTest]
        public IEnumerator Update3DPositionWithoutConnectToPositionalChannel() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            client.Update3DPosition(Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to update 3D position due to unconnected positional channel");
        });

        [UnityTest]
        public IEnumerator AudioEnergyChanged() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.Login(authConfig);
            await UniTask.WaitUntil(() => onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            client.Connect(channelConfig);
            await UniTask.WaitUntil(() => onUserConnected);

            await UniTask.WaitUntil(() => onAudioEnergyChanged);
            Assert.AreNotEqual(0, changedAudioEnergy.ReceivedValue);
        });
    }
}
