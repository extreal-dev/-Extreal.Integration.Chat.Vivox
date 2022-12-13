using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxAppConfigProvider : MonoBehaviour
    {
        [SerializeField] private VivoxAppConfig vivoxAppConfig;

        public VivoxAppConfig VivoxAppConfig => vivoxAppConfig;
    }
}
