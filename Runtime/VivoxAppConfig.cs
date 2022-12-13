using UnityEngine;

namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Class that holds the application config for Vivox.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Extreal/Integration.Chat.Vivox/" + nameof(VivoxAppConfig),
        fileName = nameof(VivoxAppConfig))]
    public class VivoxAppConfig : ScriptableObject
    {
        [SerializeField] private string apiEndPoint;
        [SerializeField] private string domain;
        [SerializeField] private string issuer;
        [SerializeField] private string secretKey;

        /// <summary>
        /// Uses to create a client.
        /// </summary>
        /// <value>API end point of Vivox API information.</value>
        public string ApiEndPoint => apiEndPoint;

        /// <summary>
        /// Uses to create an account ID and a channel ID.
        /// </summary>
        /// <value>Domain of Vivox API information.</value>
        public string Domain => domain;

        /// <summary>
        /// Uses to create an account ID and a channel ID.
        /// </summary>
        /// <value>Issuer of Vivox API information.</value>
        public string Issuer => issuer;

        /// <summary>
        /// Uses to create an account ID and a channel ID.
        /// </summary>
        /// <value>Secret key of Vivox API information.</value>
        public string SecretKey => secretKey;
    }
}
