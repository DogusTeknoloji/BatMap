using System;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using NUnit.Framework;

namespace BatMap.Tests {

    [TestFixture]
    public class ConfigurationTests {

        [Test]
        public void Unregistered_Throws_Exception() {
            var config = new MapConfiguration();

            try {
                config.Map<CustomerDTO>(new Customer());

                Assert.Fail("Should have thrown InvalidOperationException.");
            }
            catch (InvalidOperationException) {
                Assert.Pass();
            }
        }
    }
}
