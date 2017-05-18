using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace BatMap.Tests.DTO {

    public class ForTest1DTO {
        [ExcludeFromCodeCoverage]
        public IList<AddressDTO> Address { get; set; }
    }

    public class ForTest2DTO {
        public int Number { get; set; }
    }

    public class ForTest3DTO {
        public CityDTO[] Cities { get; set; }
    }

    public class ForTest4DTO {
        public Dictionary<int, OrderDTO> Orders { get; set; }
    }

    public class ForTest5DTO {
        public Collection<CityDTO> Cities { get; set; }
    }
}
