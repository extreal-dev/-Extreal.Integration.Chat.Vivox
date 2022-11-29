using System;

namespace Extreal.Integration.Chat.Vivox
{
    public struct VivoxAuthConfig
    {
        public string DisplayName { get; }
        public string AccountName { get; }
        public TimeSpan TokenExpirationDuration { get; }

        public VivoxAuthConfig(string displayName, string accountName = default, TimeSpan tokenExpirationDuration = default)
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
