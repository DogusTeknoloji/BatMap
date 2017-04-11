using System.Collections.Generic;

namespace BatMap.Benchmark.DTO {

    public class CustomerDTO {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public double Endorsement { get; set; }
        public List<AddressDTO> Addresses { get; set; }
        public List<OrderDTO> Orders { get; set; }
    }
}
