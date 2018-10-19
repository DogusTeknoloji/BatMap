using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BatMap.Tests.DTO {

    public class ForTest1DTO {
        [ExcludeFromCodeCoverage]
        public IList<AddressDTO> Address { get; set; }
    }

    public class ForTest2DTO {
        public int Number { get; set; }
        public int? Number2 { get; set; }
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

    public class ForTest6DTO {
        public HashSet<CityDTO> Cities { get; set; }
    }

    public class ForTest7DTO {
        public int[] Image1 { get; set; }
        public List<byte?> Image2 { get; set; }
        public Collection<byte> Image3 { get; set; }
    }
    
    public class ForTest8DTO {
        
        public ForTest8DTO(int id, string name)
        {
            Id = id;
            Name = name;
        }
   
        public int Id { get; }
        public string Name { get; }
    }
}
