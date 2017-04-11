using System.Collections.Generic;

namespace BatMap.Tests.DTO {

    public class CompanyDTO {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public AddressDTO Address { get; set; }
        public IList<AddressDTO> Addresses;
    }
}
