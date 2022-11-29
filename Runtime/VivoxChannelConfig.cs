using System;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox
{
    public struct VivoxChannelConfig
    {
        public string ChannelName { get; }
        public ChatType ChatType { get; }
        public ChannelType ChannelType { get; }
        public Channel3DProperties Properties { get; }
        public bool TransmissionSwitch { get; }
        public TimeSpan TokenExpirationDuration { get; }

        public VivoxChannelConfig
        (
            string channelName,
            ChatType chatType = default,
            ChannelType channelType = default,
            bool transmissionSwitch = true,
            TimeSpan tokenExpirationDuration = default

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
        }
    }
}
