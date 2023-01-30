using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extreal.Core.Logging;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VivoxUnity;


namespace Extreal.Integration.Chat.Vivox.Test.Sub
{
    public class VivoxClientTestSub
    {
        private VivoxClient client;

        private bool onLoggedIn;
        private bool onLoggedOut;

        private bool onUserConnected;
        private readonly List<IParticipant> connectedUsers = new List<IParticipant>();

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

            onUserConnected = default;
            connectedUsers.Clear();

            _ = client.OnLoggedIn
                .Subscribe(_ => onLoggedIn = true)
                .AddTo(disposables);

            _ = client.OnLoggedOut
                .Subscribe(_ => onLoggedOut = true)
                .AddTo(disposables);

            _ = client.OnUserConnected
                .Subscribe(connectedUser =>
                {
                    onUserConnected = true;
                    connectedUsers.Add(connectedUser);
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

        [UnityTest]
        public IEnumerator UserConnectedSub() => UniTask.ToCoroutine(async () =>
        {
            const string displayName = "TestSubUser";
            var authConfig = new VivoxAuthConfig(displayName);
            await client.LoginAsync(authConfig);
            Assert.IsTrue(onLoggedIn);

            const string channelName = "TestChannel";
            var channelConfig = new VivoxChannelConfig(channelName);
            await client.ConnectAsync(channelConfig);
            Assert.IsTrue(onUserConnected);

            await UniTask.Delay(TimeSpan.FromSeconds(5));
            Assert.AreEqual(1, connectedUsers.Count);
        });
    }
}
