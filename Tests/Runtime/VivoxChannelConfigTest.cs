using System;
using NUnit.Framework;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxChannelConfigTest
    {
        [Test]
        public void NewVivoxChannelConfigWithChannelNameNull()
            => Assert.That(() => _ = new VivoxChannelConfig(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelName"));

        [Test]
        public void NewVivoxChannelConfigWithNotExistedChatCapability()
            => Assert.That(() => _ = new VivoxChannelConfig("TestChannel", Enum.Parse<ChatType>("10")),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contain($"'10' is not defined in {nameof(ChatType)}"));

        [Test]
        public void NewVivoxChannelConfigWithNotExistedChannelType()
            => Assert.That(() => _ = new VivoxChannelConfig("TestChannel", channelType: Enum.Parse<ChannelType>("10")),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contain($"'10' is not defined in {nameof(ChannelType)}"));
    }
}
