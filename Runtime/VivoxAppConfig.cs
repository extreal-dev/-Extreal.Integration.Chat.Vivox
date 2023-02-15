using System;
using Extreal.Core.Common.Retry;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Class that holds the application config for Vivox.
    /// </summary>
    public class VivoxAppConfig
    {
        /// <summary>
        /// Uses to create a client.
        /// </summary>
        /// <value>API end point of Vivox API information.</value>
        public string ApiEndPoint { get; }

        /// <summary>
        /// Uses to create an account ID and a channel ID.
        /// </summary>
        /// <value>Domain of Vivox API information.</value>
        public string Domain { get; }

        /// <summary>
        /// Uses to create an account ID and a channel ID.
        /// </summary>
        /// <value>Issuer of Vivox API information.</value>
        public string Issuer { get; }

        /// <summary>
        /// Uses to create an account ID and a channel ID.
        /// </summary>
        /// <value>Secret key of Vivox API information.</value>
        public string SecretKey { get; }

        /// <summary>
        /// Uses to initialize a VivoxClient.
        /// </summary>
        /// <value>Vivox configuration.</value>
        public VivoxConfig VivoxConfig { get; }

        /// <summary>
        /// Uses for login retry.
        /// </summary>
        /// <value>Retry strategy to use for logging to the server.</value>
        public IRetryStrategy LoginRetryStrategy { get; }

        /// <summary>
        /// Creates a new VivoxAppConfig with given apiEndPoint, domain, issuer and secretKey.
        /// </summary>
        /// <param name="apiEndPoint">API end point of Vivox API information.</param>
        /// <param name="domain">Domain of Vivox API information.</param>
        /// <param name="issuer">Issuer of Vivox API information.</param>
        /// <param name="secretKey">Secret key of Vivox API information.</param>
        /// <param name="vivoxConfig">Vivox configuration.</param>
        /// <param name="loginRetryStrategy">Retry strategy to use for logging to the server.</param>
        /// <exception cref="ArgumentNullException">If 'apiEndPoint'/'domain'/'issuer'/'secretKey' is null.</exception>
        public VivoxAppConfig(
            string apiEndPoint, string domain, string issuer, string secretKey,
            VivoxConfig vivoxConfig = null, IRetryStrategy loginRetryStrategy = null)
        {
            if (string.IsNullOrEmpty(apiEndPoint))
            {
                throw new ArgumentNullException(nameof(apiEndPoint));
            }
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentNullException(nameof(domain));
            }
            if (string.IsNullOrEmpty(issuer))
            {
                throw new ArgumentNullException(nameof(issuer));
            }
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException(nameof(secretKey));
            }

            ApiEndPoint = apiEndPoint;
            Domain = domain;
            Issuer = issuer;
            SecretKey = secretKey;
            VivoxConfig = vivoxConfig;
            LoginRetryStrategy = loginRetryStrategy ?? NoRetryStrategy.Instance;
        }
    }
}
