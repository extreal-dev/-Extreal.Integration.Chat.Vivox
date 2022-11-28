using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.ChatControl
{
    public class ChatControlScope : LifetimeScope
    {
        [SerializeField] private VivoxConnectionConfig vivoxConnectionConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(vivoxConnectionConfig);
            builder.Register<VivoxClient>(Lifetime.Singleton);
            builder.Register<ChatControlModel>(Lifetime.Singleton);

            builder.RegisterEntryPoint<ChatControlPresenter>();
        }
    }
}
