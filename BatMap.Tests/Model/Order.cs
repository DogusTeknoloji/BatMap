using System.Collections.Generic;

namespace BatMap.Tests.Model {

    public class Order {
        public int Id { get; set; }
        public string OrderNo { get; set; }
        public double Price;
        public IList<OrderDetail> OrderDetails { get; set; }
    }
}
