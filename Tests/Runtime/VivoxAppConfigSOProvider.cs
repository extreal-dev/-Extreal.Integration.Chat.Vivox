using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxAppConfigSOProvider : MonoBehaviour
    {
        [SerializeField] private VivoxAppConfigSO vivoxAppConfigSO;

        public VivoxAppConfigSO VivoxAppConfigSO => vivoxAppConfigSO;
    }
}
