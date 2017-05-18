using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace BatMap.Tests.Model {

    public class ForTest1 {
        [ExcludeFromCodeCoverage]
        public Address Address { get; set; }
    }

    public class ForTest2 {
        public int? Number { get; set; }
    }

    public class ForTest3 {
        public City[] Cities { get; set; }
    }

    public class ForTest4 {
        public Dictionary<int, Order> Orders { get; set; }
    }

    public class ForTest5 {
        public Collection<City> Cities { get; set; }
    }
}
