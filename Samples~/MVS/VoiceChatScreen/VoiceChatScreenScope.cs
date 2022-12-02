using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.VoiceChatScreen
{
    public class VoiceChatScreenScope : LifetimeScope
    {
        [SerializeField] private VoiceChatScreenView voiceChatScreenView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(voiceChatScreenView);
            builder.Register<VoiceChatScreenModel>(Lifetime.Singleton);

            builder.RegisterEntryPoint<VoiceChatScreenPresenter>();
        }
    }
}
