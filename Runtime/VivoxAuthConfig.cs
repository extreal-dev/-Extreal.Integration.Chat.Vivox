using System;

namespace Extreal.Integration.Chat.Vivox
{
    public struct VivoxAuthConfig
    {
        public string DisplayName { get; }
        public string AccountName { get; }
        public byte TokenExpirationDuration { get; }

        public VivoxAuthConfig(string displayName, string accountName = default, byte tokenExpirationDuration = 60)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            DisplayName = displayName;
            AccountName = string.IsNullOrEmpty(accountName) ? Guid.NewGuid().ToString() : accountName;
            TokenExpirationDuration = tokenExpirationDuration;
        }
    }
}
