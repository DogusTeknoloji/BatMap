using System.Collections.Generic;

namespace BatMap.Tests.DTO {

    public class ForTest1DTO {
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
}
