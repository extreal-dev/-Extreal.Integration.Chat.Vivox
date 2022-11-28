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
        public byte TokenExpirationDuration { get; }

        public VivoxChannelConfig
        (
            string channelName,
            ChatType chatType = default,
            ChannelType channelType = default,
            bool transmissionSwitch = true,
            byte tokenExpirationDuration = 60

        )
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentNullException(nameof(channelName));
            }
            if (!Enum.IsDefined(typeof(ChatType), chatType))
            {
                throw new ArgumentOutOfRangeException(nameof(chatType), $"'{chatType}' is not defined in {nameof(Vivox.ChatType)}");
            }
            if (!Enum.IsDefined(typeof(ChannelType), channelType))
            {
                throw new ArgumentOutOfRangeException(nameof(channelType), $"'{channelType}' is not defined in {nameof(VivoxUnity.ChannelType)}");
            }

            ChannelName = channelName;
            ChatType = chatType;
            ChannelType = channelType;
            Properties = ChannelType is ChannelType.Positional ? new Channel3DProperties() : null;
            TransmissionSwitch = transmissionSwitch;
            TokenExpirationDuration = tokenExpirationDuration;
        }
    }
}
