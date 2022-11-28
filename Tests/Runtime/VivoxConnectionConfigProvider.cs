using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxConnectionConfigProvider : MonoBehaviour
    {
        [SerializeField] private VivoxConnectionConfig vivoxConnectionConfig;

        public VivoxConnectionConfig VivoxConnectionConfig => vivoxConnectionConfig;
    }
}
