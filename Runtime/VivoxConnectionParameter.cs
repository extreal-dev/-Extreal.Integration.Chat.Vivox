using System;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox
{
    public struct VivoxConnectionParameter
    {
        public string ChannelName { get; }
        public ChatCapability ChatCapability { get; }
        public ChannelType ChannelType { get; }
        public Channel3DProperties Properties { get; }
        public bool TransmissionSwitch { get; }
        public byte TokenExpirationDuration { get; }

        public VivoxConnectionParameter
        (
            string channelName,
            ChatCapability chatCapability = default,
            ChannelType channelType = default,
            bool transmissionSwitch = true,
            byte tokenExpirationDuration = 60

        )
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }
            if (!Enum.IsDefined(typeof(ChatCapability), chatCapability))
            {
                throw new ArgumentOutOfRangeException(nameof(chatCapability), $"'{chatCapability}' is not defined in {nameof(ChatCapability)}");
            }
            if (!Enum.IsDefined(typeof(ChannelType), channelType))
            {
                throw new ArgumentOutOfRangeException(nameof(channelType), $"'{channelType}' is not defined in {nameof(ChannelType)}");
            }

            ChannelName = channelName;
            ChatCapability = chatCapability;
            ChannelType = channelType;
            Properties = ChannelType is ChannelType.Positional ? new Channel3DProperties() : null;
            TransmissionSwitch = transmissionSwitch;
            TokenExpirationDuration = tokenExpirationDuration;
        }
    }
}
