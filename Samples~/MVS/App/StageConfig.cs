using Extreal.Core.StageNavigation;
using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.MVS.App
{
    [CreateAssetMenu(
        menuName = "Config/" + nameof(StageConfig),
        fileName = nameof(StageConfig))]
    public class StageConfig : StageConfigBase<StageName, SceneName>
    {
    }
}
