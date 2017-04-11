using System.Collections.Generic;

namespace BatMap.Benchmark.Model {

    public class Customer {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Phone { get; set; }
        public double Endorsement { get; set; }
        public List<Address> Addresses { get; set; }
        public List<Order> Orders { get; set; }
    }
}
