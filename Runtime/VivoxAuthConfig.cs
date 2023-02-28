using System;

namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Class that holds the authentication config for Vivox.
    /// </summary>
    public readonly struct VivoxAuthConfig
    {
        /// <summary>
        /// Uses to create an account ID.
        /// </summary>
        /// <value>Display name of the account.</value>
        public string DisplayName { get; }

        /// <summary>
        /// Uses to create an account ID.
        /// </summary>
        /// <value>Name of the account.</value>
        public string AccountName { get; }

        /// <summary>
        /// Uses to get the login token.
        /// </summary>
        /// <value>Expiration duration of the token.</value>
        public TimeSpan TokenExpirationDuration { get; }

        /// <summary>
        /// Creates a new VivoxAuthConfig with given displayName, accountName and tokenExpirationDuration.
        /// </summary>
        /// <param name="displayName">Display name of the account.</param>
        /// <param name="accountName">
        ///     <para>Name of the account.</para>
        ///     Default: GUID
        /// </param>
        /// <param name="tokenExpirationDuration">
        ///     <para>Expiration duration of the token.</para>
        ///     Default: 60 seconds
        /// </param>
        /// <exception cref="ArgumentNullException">If 'displayName' is null.</exception>
        public VivoxAuthConfig
        (
            string displayName,
            string accountName = default,
            TimeSpan tokenExpirationDuration = default
        )
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            DisplayName = displayName;
            AccountName = string.IsNullOrEmpty(accountName) ? Guid.NewGuid().ToString() : accountName;
            TokenExpirationDuration
                = tokenExpirationDuration == default ? TimeSpan.FromSeconds(60) : tokenExpirationDuration;
        }
    }
}
