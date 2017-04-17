using System.Data.Entity;

namespace BatMap.Tests.Model {

    public class TestEntities: DbContext {
        public IDbSet<Address> Addresses { get; set; }
        public IDbSet<City> Cities { get; set; }
        public IDbSet<Customer> Customers { get; set; }
        public IDbSet<Order> Orders { get; set; }
        public IDbSet<OrderDetail> OrderDetails { get; set; }
        public IDbSet<Product> Products { get; set; }
    }
}
