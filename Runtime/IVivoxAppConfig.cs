namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Interface for implementation holding the application config for Vivox.
    /// </summary>
    public interface IVivoxAppConfig
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
    }
}
