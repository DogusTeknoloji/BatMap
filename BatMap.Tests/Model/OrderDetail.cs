namespace BatMap.Tests.Model {

    public class OrderDetail {
        public int Id { get; set; }
        public Product Product;
        public int Count { get; set; }
        public double UnitPrice { get; set; }
    }
}
