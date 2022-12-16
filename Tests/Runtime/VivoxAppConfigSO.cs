using UnityEngine;

namespace Extreal.Integration.Chat.Vivox.Test
{
    [CreateAssetMenu(
        menuName = "Config/" + nameof(VivoxAppConfigSO),
        fileName = nameof(VivoxAppConfigSO))]
    public class VivoxAppConfigSO : ScriptableObject
    {
        [SerializeField] private string apiEndPoint;
        [SerializeField] private string domain;
        [SerializeField] private string issuer;
        [SerializeField] private string secretKey;

        public string ApiEndPoint => apiEndPoint;
        public string Domain => domain;
        public string Issuer => issuer;
        public string SecretKey => secretKey;
    }
}
