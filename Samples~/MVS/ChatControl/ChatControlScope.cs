using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.ChatControl
{
    public class ChatControlScope : LifetimeScope
    {
        [SerializeField] private VivoxAppConfig vivoxAppConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(vivoxAppConfig);
            builder.Register<VivoxClient>(Lifetime.Singleton);
            builder.Register<ChatControlModel>(Lifetime.Singleton);

            builder.RegisterEntryPoint<ChatControlPresenter>();
        }
    }
}
