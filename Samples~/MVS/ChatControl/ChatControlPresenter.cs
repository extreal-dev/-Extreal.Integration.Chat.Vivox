using System;
using Extreal.Core.StageNavigation;
using Extreal.Integration.Chat.Vivox.MVS.App;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.ChatControl
{
    public class ChatControlPresenter : IInitializable, IDisposable
    {
        private readonly IStageNavigator<StageName> stageNavigator;
        private readonly ChatControlModel chatControlModel;

        public ChatControlPresenter
        (
            IStageNavigator<StageName> stageNavigator,
            ChatControlModel chatControlModel
        )
        {
            this.stageNavigator = stageNavigator;
            this.chatControlModel = chatControlModel;
        }

        public void Initialize()
        {
            chatControlModel.Initialize();
            stageNavigator.OnStageTransitioned += chatControlModel.OnStageTransitioned;
        }

        public void Dispose()
        {
            stageNavigator.OnStageTransitioned -= chatControlModel.OnStageTransitioned;
            GC.SuppressFinalize(this);
        }
    }
}
