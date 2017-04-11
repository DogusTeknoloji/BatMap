using System.Collections.Generic;

namespace BatMap.Tests.DTO {

    public class OrderDTO {
        public int Id { get; set; }
        public string OrderNo;
        public double Price { get; set; }
        public ICollection<OrderDetailDTO> OrderDetails { get; set; }
    }
}
