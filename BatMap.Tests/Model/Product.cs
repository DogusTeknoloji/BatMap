namespace BatMap.Tests.Model {

    public class Product {
        public int Id { get; set; }
        public string Name { get; set; }
        public Company Supplier { get; set; }
    }
}
