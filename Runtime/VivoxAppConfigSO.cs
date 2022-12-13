using UnityEngine;

namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Class that holds the application config for Vivox.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Extreal/Integration.Chat.Vivox/" + nameof(VivoxAppConfigSO),
        fileName = nameof(VivoxAppConfigSO))]
    public class VivoxAppConfigSO : ScriptableObject, IVivoxAppConfig
    {
        [SerializeField] private string apiEndPoint;
        [SerializeField] private string domain;
        [SerializeField] private string issuer;
        [SerializeField] private string secretKey;

        /// <inheritdoc/>
        public string ApiEndPoint => apiEndPoint;

        /// <inheritdoc/>
        public string Domain => domain;

        /// <inheritdoc/>
        public string Issuer => issuer;

        /// <inheritdoc/>
        public string SecretKey => secretKey;
    }
}
