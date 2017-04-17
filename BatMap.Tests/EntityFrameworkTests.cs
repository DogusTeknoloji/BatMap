using System;
using System.Collections.Generic;
using System.Linq;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace BatMap.Tests {

    [TestFixture]
    public class EntityFrameworkTests {

        [Test]
        public void GetAllBlogs_orders_by_name() {
            var context = new TestEntities();
        }
    }
}
