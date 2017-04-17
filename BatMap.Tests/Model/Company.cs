using System.Collections.Generic;

namespace BatMap.Tests.Model {

    public class Company {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public Address MainAddress { get; set; }
        public int MainAddressId { get; set; }
        public ICollection<Address> Addresses { get; set; }
    }
}
