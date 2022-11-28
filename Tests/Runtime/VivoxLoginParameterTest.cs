using System;
using NUnit.Framework;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxLoginParameterTest
    {
        [Test]
        public void NewVivoxLoginParameterWithDisplayNameNull()
            => Assert.That(() => _ = new VivoxLoginParameter(null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("displayName"));
    }
}
