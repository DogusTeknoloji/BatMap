using System.Collections.Generic;

namespace BatMap.Benchmark.DTO {

    public class OrderDTO {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public double Price { get; set; }
        public List<OrderDetailDTO> OrderDetails { get; set; }
    }
}
