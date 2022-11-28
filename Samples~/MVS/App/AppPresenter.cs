using Extreal.Core.StageNavigation;
using VContainer.Unity;

namespace Extreal.Integration.Chat.Vivox.MVS.App
{
    public class AppPresenter : IStartable
    {
        private readonly IStageNavigator<StageName> stageNavigator;

        public AppPresenter(IStageNavigator<StageName> stageNavigator)
            => this.stageNavigator = stageNavigator;

        public void Start()
            => stageNavigator.ReplaceAsync(StageName.ChatStage);
    }
}
