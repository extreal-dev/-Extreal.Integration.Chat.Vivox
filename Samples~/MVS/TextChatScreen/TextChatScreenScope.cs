using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.TextChatScreen
{
    public class TextChatScreenScope : LifetimeScope
    {
        [SerializeField] private TextChatScreenView textChatScreenView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(textChatScreenView);
            builder.Register<TextChatScreenModel>(Lifetime.Singleton);

            builder.RegisterEntryPoint<TextChatScreenPresenter>();
        }
    }
}
