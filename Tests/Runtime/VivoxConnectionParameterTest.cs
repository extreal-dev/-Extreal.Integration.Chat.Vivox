using System;
using NUnit.Framework;
using VivoxUnity;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxConnectionParameterTest
    {
        [Test]
        public void NewVivoxConnectionParameterWithChannelNameNull()
            => Assert.That(() => _ = new VivoxConnectionParameter(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelName"));

        [Test]
        public void NewVivoxConnectionParameterWithNotExistedChatCapability()
            => Assert.That(() => _ = new VivoxConnectionParameter("TestChannel", Enum.Parse<ChatCapability>("10")),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contain($"'10' is not defined in {nameof(ChatCapability)}"));

        [Test]
        public void NewVivoxConnectionParameterWithNotExistedChannelType()
            => Assert.That(() => _ = new VivoxConnectionParameter("TestChannel", channelType: Enum.Parse<ChannelType>("10")),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contain($"'10' is not defined in {nameof(ChannelType)}"));
    }
}
