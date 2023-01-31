using System;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox
{
    /// <summary>
    /// Class that holds the channel config for Vivox.
    /// </summary>
    public readonly struct VivoxChannelConfig
    {
        /// <summary>
        /// Uses to create a channel ID.
        /// </summary>
        /// <value>Name of the channel.</value>
        public string ChannelName { get; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Chat type to be used in connection.</value>
        public ChatType ChatType { get; }

        /// <summary>
        /// Uses to create a channel ID.
        /// </summary>
        /// <value>Type of the channel.</value>
        public ChannelType ChannelType { get; }

        /// <summary>
        /// Uses to create a channel ID.
        /// </summary>
        /// <value>Property of the channel.</value>
        public Channel3DProperties Properties { get; }

        /// <summary>
        /// Uses in connection.
        /// </summary>
        /// <value>Transmission switch to be used in connection.</value>
        public bool TransmissionSwitch { get; }

        /// <summary>
        /// Uses to get the connection token.
        /// </summary>
        /// <value>Expiration duration of the token.</value>
        public TimeSpan TokenExpirationDuration { get; }

        /// <summary>
        /// Uses when connection is not successful.
        /// </summary>
        /// <value>Time to wait when connection is not successful.</value>
        public TimeSpan Timeout { get; }

        /// <summary>
        /// Creates a new VivoxChannelConfig with given channelName, chatType, channelType, transmissionSwitch and tokenExpirationDuration.
        /// </summary>
        /// <param name="channelName">Name of the channel.</param>
        /// <param name="chatType">Chat type to be used in connection.</param>
        /// <param name="channelType">Type of the channel.</param>
        /// <param name="transmissionSwitch">Transmission switch to be used in connection.</param>
        /// <param name="tokenExpirationDuration">
        ///     <para>Expiration duration of the token.</para>
        ///     Default: 60 seconds
        /// </param>
        /// <param name="timeout">
        ///     <para>Time to wait when connection is not successful.</para>
        ///     Default: 10 seconds
        /// </param>
        /// <exception cref="ArgumentNullException">If 'channelName' is null.</exception>
        public VivoxChannelConfig
        (
            string channelName,
            ChatType chatType = default,
            ChannelType channelType = default,
            bool transmissionSwitch = true,
            TimeSpan tokenExpirationDuration = default,
            TimeSpan timeout = default
        )
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }

            ChannelName = channelName;
            ChatType = chatType;
            ChannelType = channelType;
            Properties = ChannelType is ChannelType.Positional ? new Channel3DProperties() : null;
            TransmissionSwitch = transmissionSwitch;
            TokenExpirationDuration
                = tokenExpirationDuration == default ? TimeSpan.FromSeconds(60) : tokenExpirationDuration;
            Timeout = timeout == default ? TimeSpan.FromSeconds(10) : timeout;
        }
    }
}
