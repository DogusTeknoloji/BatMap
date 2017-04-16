using System.Collections.Generic;

namespace BatMap.Tests.Model {

    public class Customer: Company {

        public Customer() {
            Orders = new List<Order>();
        }

        public double Endorsement { get; set; }
        public List<Order> Orders { get; set; }
    }
}
