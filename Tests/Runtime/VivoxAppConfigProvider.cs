using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxAppConfigProvider : MonoBehaviour
    {
        [SerializeField] private VivoxAppConfigSO vivoxAppConfigSO;

        public VivoxAppConfigSO VivoxAppConfigSO => vivoxAppConfigSO;
    }
}
