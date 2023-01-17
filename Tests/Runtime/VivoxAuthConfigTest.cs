using System;
using NUnit.Framework;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxAuthConfigTest
    {
        [Test]
        public void NewVivoxAuthConfigWithDisplayNameNull()
            => Assert.That(() => _ = new VivoxAuthConfig(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("displayName"));
    }
}
