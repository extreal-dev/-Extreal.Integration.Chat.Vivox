using System;
using NUnit.Framework;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxChannelConfigTest
    {
        [Test]
        public void NewVivoxChannelConfigWithChannelNameNull()
            => Assert.That(() => _ = new VivoxChannelConfig(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("channelName"));
    }
}
