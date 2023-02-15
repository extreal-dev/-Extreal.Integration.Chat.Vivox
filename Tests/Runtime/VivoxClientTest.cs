using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extreal.Core.Common.Retry;
using Extreal.Core.Logging;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VivoxUnity;
using Object = UnityEngine.Object;

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

        private int onConnectRetrying;
        private bool onConnectRetried;
        private bool isInvokeOnConnectRetried;

        [SuppressMessage("CodeCracker", "CC0033")]
        private readonly CompositeDisposable disposables = new CompositeDisposable();

        [OneTimeSetUp]
        public void OneTimeSetUp()
            => LoggingManager.Initialize(LogLevel.Debug);

        [UnitySetUp]
        public IEnumerator InitializeAsync() => UniTask.ToCoroutine(async () =>
        {
            await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);

            await SceneManager.LoadSceneAsync("Main");

            InitializeClient();
        });

        private void InitializeClient(VivoxConfig vivoxConfig = null, IRetryStrategy loginRetryStrategy = null)
        {
            DisposeClient();

            var chatConfigProvider = Object.FindObjectOfType<ChatConfigProvider>();
            var appConfig = chatConfigProvider.ChatConfig.ToVivoxAppConfig(vivoxConfig, loginRetryStrategy);

            client = new VivoxClient(appConfig);

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

            onConnectRetrying = 0;
            onConnectRetried = false;
            isInvokeOnConnectRetried = false;

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

            _ = client.OnConnectRetrying
                .Subscribe(retryCount => onConnectRetrying = retryCount)
                .AddTo(disposables);

            _ = client.OnConnectRetried
                .Subscribe(retryResult =>
                {
                    isInvokeOnConnectRetried = true;
                    onConnectRetried = retryResult;
                })
                .AddTo(disposables);
        }

        private void DisposeClient()
        {
            client?.Dispose();
            disposables.Clear();
        }

        [UnityTearDown]
        public IEnumerator DisposeAsync() => UniTask.ToCoroutine(async () =>
        {
            if (client.LoginSession?.State == LoginState.LoggedIn)
            {
                client.Logout();
                await UniTask.WaitUntil(() => onLoggedOut);
            }

            DisposeClient();
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
        public IEnumerator LoginSuccessForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy());
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);
            Assert.IsTrue(onRecoveryStateChanged);
            Assert.AreEqual(ConnectionRecoveryState.Connected, changedRecoveryState);
            Assert.AreEqual(0, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);
        });

        [UnityTest]
        public IEnumerator LoginSuccessWithVivoxConfigLogLevelDebug() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.Expect(LogType.Log, new Regex(".*VivoxApi.*"));

            var vivoxConfig = new VivoxConfig
            {
                InitialLogLevel = vx_log_level.log_debug
            };
            InitializeClient(vivoxConfig);

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

        private static async UniTask DisableInternetConnectionAsync()
        {
            Debug.Log("<color=red>Disable the Internet connection</color>");
            await UniTask.WaitUntil(() => Application.internetReachability == NetworkReachability.NotReachable);
        }

        private const int Wait = 5;
        private static async UniTask EnableInternetConnectionAsync(bool strong = false)
        {
            if (strong)
            {
                await UniTask.WaitUntil(() =>
                {
                    Debug.Log("<color=lime>Enable the Internet connection</color>");
                    return Application.internetReachability != NetworkReachability.NotReachable;
                });
            }
            else
            {
                Debug.Log("<color=lime>Enable the Internet connection</color>");
                await UniTask.WaitUntil(() => Application.internetReachability != NetworkReachability.NotReachable);
                await UniTask.Delay(TimeSpan.FromSeconds(Wait));
            }
        }

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnection() => UniTask.ToCoroutine(async () =>
        {
            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            await DisableInternetConnectionAsync();

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            Exception exception = null;
            try
            {
                await client.LoginAsync(authConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(VivoxConnectionException), exception.GetType());
            Assert.AreEqual("The login failed", exception.Message);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnectionForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            await DisableInternetConnectionAsync();

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            Exception exception = null;
            try
            {
                await client.LoginAsync(authConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(VivoxConnectionException), exception.GetType());
            Assert.AreEqual("The login failed", exception.Message);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsTrue(isInvokeOnConnectRetried);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator DisconnectedWhileLoggedInForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            // Login once
            await client.LoginAsync(authConfig);

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsTrue(isInvokeOnConnectRetried);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator DisconnectedWhileLoggedInForRetryCancel() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            using var cts = new CancellationTokenSource();
            using var disposable = client.OnConnectRetrying.Subscribe(i =>
            {
                if (i == 3)
                {
                    cts.Cancel();
                }
            });

            // Login once
            await client.LoginAsync(authConfig, cts.Token);

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => onConnectRetrying == 3);
            Assert.IsFalse(isInvokeOnConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator DisconnectedWhileLoggedInForAutoRecoverySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            // Login once
            await client.LoginAsync(authConfig);

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.Recovering);

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.Recovered);

            Assert.IsFalse(isInvokeOnConnectRetried);
            Assert.IsNotNull(client.LoginSession);
            Assert.IsTrue(client.LoginSession.State == LoginState.LoggedIn);
        });

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnectionForRetryFailureAfterLoginAndLogoutOnce() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            // Login and logout once
            await client.LoginAsync(authConfig);
            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

            await DisableInternetConnectionAsync();

            Exception exception = null;
            try
            {
                await client.LoginAsync(authConfig);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(VivoxConnectionException), exception.GetType());
            Assert.AreEqual("The login failed", exception.Message);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsTrue(isInvokeOnConnectRetried);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnectionForRetryCancel() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            await DisableInternetConnectionAsync();

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);

            using var cts = new CancellationTokenSource();
            using var disposable = client.OnConnectRetrying.Subscribe(i =>
            {
                if (i == 3)
                {
                    cts.Cancel();
                }
            });

            Exception exception = null;
            try
            {
                await client.LoginAsync(authConfig, cts.Token);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(typeof(OperationCanceledException), exception.GetType());
            Assert.AreEqual("The retry was canceled", exception.Message);
            Assert.AreEqual(3, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator LoginWithoutInternetConnectionForRetrySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            await DisableInternetConnectionAsync();

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            client.LoginAsync(authConfig).Forget();

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.IsTrue(onConnectRetried);
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

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);
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
        public IEnumerator ConnectSuccessForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy());

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
            Assert.AreEqual(0, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);
        });

        [UnityTest]
        public IEnumerator ConnectSuccessWithMultipleChannelsForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy());

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => IsAllChannelsConnected(3)).Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual(3, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }
            Assert.AreEqual(0, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);
        });

        private bool IsAllChannelsConnected(int count)
        {
            if (client.LoginSession == null)
            {
                return false;
            }
            if (client.LoginSession.ChannelSessions.Count < count)
            {
                return false;
            }
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                if (channelSession.ChannelState != ConnectionState.Connected)
                {
                    return false;
                }
            }
            return true;
        }

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnection() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);

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
            Assert.AreEqual(typeof(VivoxConnectionException), exception.GetType());
            Assert.AreEqual("The connection failed", exception.Message);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnectionForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);

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
            Assert.AreEqual(typeof(VivoxConnectionException), exception.GetType());
            Assert.AreEqual("The connection failed", exception.Message);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnectionForRetrySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);

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
            Assert.AreEqual(typeof(VivoxConnectionException), exception.GetType());
            Assert.AreEqual("The connection failed", exception.Message);

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => onUserConnected);
            Assert.IsTrue(onConnectRetried);
            Assert.IsTrue(onChannelSessionAdded);
            Assert.AreEqual(displayName, connectedUser.Account.DisplayName);
            Assert.AreEqual(authConfig.AccountName, connectedUser.Account.Name);
            Assert.AreEqual(channelName, addedChannelId.Name);
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnectionWithMultipleChannelsForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.IsNull(client.LoginSession);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnectionWithMultipleChannelsForRetryCancel() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));
            LogAssert.Expect(LogType.Log, new Regex("The retry was canceled"));

            using var cts = new CancellationTokenSource();
            using var disposable = client.OnConnectRetrying.Subscribe(i =>
            {
                if (i == 3)
                {
                    cts.Cancel();
                }
            });

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig, cts.Token);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => onConnectRetrying == 3);
            await UniTask.Delay(TimeSpan.FromSeconds(5));

            Assert.IsNull(client.LoginSession);
            Assert.AreEqual(3, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnectionWithMultipleChannelsForConnectCancel() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));
            LogAssert.Expect(LogType.Log, new Regex("Cancel connection because it was canceled. channel: TestChannel1"));
            LogAssert.Expect(LogType.Log, new Regex("Cancel connection because it was canceled. channel: TestChannel3"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);

            using var cts = new CancellationTokenSource();

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0]), cts.Token).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2]), cts.Token).Forget();

            cts.Cancel();
            channelNames.Remove("TestChannel1");
            channelNames.Remove("TestChannel3");

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            await UniTask.WaitUntil(() => IsAllChannelsConnected(1)).Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual(1, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }
            Assert.IsTrue(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator ConnectWithoutInternetConnectionWithMultipleChannelsForRetrySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            await UniTask.WaitUntil(() => IsAllChannelsConnected(3)).Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual(3, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }
            Assert.IsTrue(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator ConnectWithoutLogin() => UniTask.ToCoroutine(async () =>
        {
            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            var channelId = await client.ConnectAsync(channelConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] Unable to connect before login");
            Assert.IsNull(channelId);
        });

        [UnityTest]
        public IEnumerator ConnectWithoutLoginIfLoggedInBefore() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

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
        public IEnumerator ConnectWithoutLoginWithMultipleChannelsIfLoggedInBeforeForRetry() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => IsAllChannelsConnected(3));

            Assert.AreEqual(3, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }
            Assert.AreEqual(0, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);
        });

        [UnityTest]
        public IEnumerator ConnectWithoutLoginAndInternetConnectionWithMultipleChannelsIfLoggedInBeforeForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

            await DisableInternetConnectionAsync();

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.IsNull(client.LoginSession);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator ConnectWithoutLoginAndInternetConnectionWithMultipleChannelsIfLoggedInBeforeForRetrySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

            await DisableInternetConnectionAsync();

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            await UniTask.WaitUntil(() => IsAllChannelsConnected(3)).Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual(3, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }
            Assert.IsTrue(onConnectRetried);
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
            var channelId1st = await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);
            Assert.IsNotNull(channelId1st);

            var channelId2nd = await client.ConnectAsync(channelConfig);
            LogAssert.Expect(LogType.Log, $"[{LogLevel.Debug}:{nameof(VivoxClient)}] This client already connected to the channel '{channelName}'");
            Assert.IsTrue(channelId1st.Equals(channelId2nd));
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
            var channelId = await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.Disconnect(channelId);
            await UniTask.WaitUntil(() => onChannelSessionRemoved);
            await UniTask.WaitUntil(() => onUserDisconnected);
            Assert.AreEqual(addedChannelId, channelId);
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
            var channelId = await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            client.Logout();
            await UniTask.WaitUntil(() => onLoggedOut);

            client.Disconnect(channelId);
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

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.Recovering);
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);
            await UniTask.WaitUntil(() => onLoggedOut);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnectForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.Recovering);
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);
            await UniTask.WaitUntil(() => onLoggedOut);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnectWithMultipleChannelsForRetryFailure() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => IsAllChannelsConnected(3));

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            Assert.IsNull(client.LoginSession);
            Assert.AreEqual(5, onConnectRetrying);
            Assert.IsFalse(onConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnectWithMultipleChannelsForRetryCancel() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));
            LogAssert.Expect(LogType.Log, new Regex("The retry was canceled"));

            using var cts = new CancellationTokenSource();
            using var disposable = client.OnConnectRetrying.Subscribe(i =>
            {
                if (i == 3)
                {
                    cts.Cancel();
                }
            });

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig, cts.Token);
            Assert.IsTrue(onLoggedIn);

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => IsAllChannelsConnected(3));

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => onConnectRetrying == 3);
            await UniTask.Delay(TimeSpan.FromSeconds(5));

            Assert.IsNull(client.LoginSession);
            Assert.AreEqual(3, onConnectRetrying);
            Assert.IsFalse(isInvokeOnConnectRetried);

            await EnableInternetConnectionAsync();
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnectWithMultipleChannelsForConnectCancel() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(5));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));
            LogAssert.Expect(LogType.Log, new Regex("Cancel connection because it was canceled. channel: TestChannel1"));
            LogAssert.Expect(LogType.Log, new Regex("Cancel connection because it was canceled. channel: TestChannel3"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            using var cts = new CancellationTokenSource();

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0]), cts.Token).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2]), cts.Token).Forget();

            await UniTask.WaitUntil(() => IsAllChannelsConnected(3));

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);

            cts.Cancel();
            channelNames.Remove("TestChannel1");
            channelNames.Remove("TestChannel3");

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            await UniTask.WaitUntil(() => IsAllChannelsConnected(1)).Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual(1, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }

            Assert.IsTrue(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnectForRetrySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            await DisableInternetConnectionAsync();

            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.Recovering);
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);
            await UniTask.WaitUntil(() => onLoggedOut);

            onUserConnected = false;
            onChannelSessionAdded = false;
            connectedUser = null;
            addedChannelId = null;

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => onUserConnected);
            Assert.IsTrue(onConnectRetried);
            Assert.IsTrue(onChannelSessionAdded);
            Assert.AreEqual(displayName, connectedUser.Account.DisplayName);
            Assert.AreEqual(authConfig.AccountName, connectedUser.Account.Name);
            Assert.AreEqual(channelName, addedChannelId.Name);
        });

        [UnityTest]
        public IEnumerator UnexpectedDisconnectWithMultipleChannelsForRetrySuccess() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            var channelNames = new List<string> { "TestChannel1", "TestChannel2", "TestChannel3" };
            client.ConnectAsync(new VivoxChannelConfig(channelNames[0])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[1])).Forget();
            client.ConnectAsync(new VivoxChannelConfig(channelNames[2])).Forget();

            await UniTask.WaitUntil(() => IsAllChannelsConnected(3));

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => isInvokeOnConnectRetried);
            await UniTask.WaitUntil(() => IsAllChannelsConnected(3)).Timeout(TimeSpan.FromSeconds(10));

            Assert.AreEqual(3, client.LoginSession.ChannelSessions.Count);
            foreach (var channelSession in client.LoginSession.ChannelSessions)
            {
                Assert.IsTrue(channelNames.Remove(channelSession.Channel.Name));
            }
            Assert.IsTrue(onConnectRetried);
        });

        [UnityTest]
        public IEnumerator ReconnectionConnectFailed() => UniTask.ToCoroutine(async () =>
        {
            InitializeClient(loginRetryStrategy: new CountingRetryStrategy(12));

            LogAssert.ignoreFailingMessages = true;
            LogAssert.Expect(LogType.Error, new Regex("Error: Name Resolution Failed \\(10006\\)"));
            LogAssert.Expect(LogType.Log, new Regex("Reconnection connect failed"));

            const string displayName = "TestUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const int max = 10;
            for (var i = 0; i < max; i++)
            {
                client.ConnectAsync(new VivoxChannelConfig($"TestChannel{i}", ChatType.TextOnly)).Forget();
            }
            await UniTask.WaitUntil(() => IsAllChannelsConnected(max));

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => client.LoginSession == null);

            await EnableInternetConnectionAsync(strong: true);

            await UniTask.WaitUntil(() => client.LoginSession?.State == LoginState.LoggedIn)
                .Timeout(TimeSpan.FromSeconds(10));

            await DisableInternetConnectionAsync();
            await UniTask.WaitUntil(() => changedRecoveryState == ConnectionRecoveryState.FailedToRecover);
            await UniTask.WaitUntil(() => client.LoginSession == null);

            await EnableInternetConnectionAsync(strong: true);
            await UniTask.Delay(TimeSpan.FromSeconds(Wait));
        });
    }
}
