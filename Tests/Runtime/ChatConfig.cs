using UnityEngine;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox.Test
{
    [CreateAssetMenu(
        menuName = "Extreal/Integration.Chat.Vivox/" + nameof(ChatConfig),
        fileName = nameof(ChatConfig))]
    public class ChatConfig : ScriptableObject
    {
        [SerializeField] private string apiEndPoint;
        [SerializeField] private string domain;
        [SerializeField] private string issuer;
        [SerializeField] private string secretKey;

        public VivoxAppConfig ToVivoxAppConfig(VivoxConfig vivoxConfig = null)
            => new VivoxAppConfig(apiEndPoint, domain, issuer, secretKey, vivoxConfig);
    }
}
