namespace BatMap.Tests.DTO {

    public class OrderDetailDTO {
        public int Id { get; set; }
        public ProductDTO Product { get; set; }
        public OrderDTO Order { get; set; }
        public double SubPrice;
    }
}
