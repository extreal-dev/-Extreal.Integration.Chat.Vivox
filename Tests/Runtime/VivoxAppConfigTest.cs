using System;
using NUnit.Framework;

namespace Extreal.Integration.Chat.Vivox.Test
{
    public class VivoxAppConfigTest
    {
        [Test]
        public void NewVivoxAppConfigWithApiEndPointNull()
            => Assert.That(() => _ = new VivoxAppConfig(null, "domain", "issuer", "secretKey"),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("apiEndPoint"));

        [Test]
        public void NewVivoxAppConfigWithDomainNull()
            => Assert.That(() => _ = new VivoxAppConfig("apiEndPoint", null, "issuer", "secretKey"),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("domain"));

        [Test]
        public void NewVivoxAppConfigWithIssuerNull()
            => Assert.That(() => _ = new VivoxAppConfig("apiEndPoint", "domain", null, "secretKey"),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("issuer"));

        [Test]
        public void NewVivoxAppConfigWithSecretKeyNull()
            => Assert.That(() => _ = new VivoxAppConfig("apiEndPoint", "domain", "issuer", null),
                Throws.TypeOf<ArgumentNullException>()
                    .With.Message.Contain("secretKey"));
    }
}
