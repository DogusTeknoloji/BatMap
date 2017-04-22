using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BatMap.Tests.DTO {

    public class CustomerDTO : CompanyDTO {
        public double Endorsement { get; set; }
        public ICollection<OrderDTO> Orders { get; set; }
        public int OrderCount {
            [ExcludeFromCodeCoverage]
            get { return Orders.Count; }
        }
    }
}
