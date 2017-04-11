namespace BatMap.Tests.DTO {

    public class ProductDTO {
        public int Id { get; set; }
        public string Name { get; set; }
        public CompanyDTO Supplier { get; set; }
    }
}
