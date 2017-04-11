using System.Collections.Generic;

namespace BatMap.Benchmark.Model {

    public class Order {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public double Price { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
    }
}
