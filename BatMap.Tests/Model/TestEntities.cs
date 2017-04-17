using System.Data.Entity;

namespace BatMap.Tests.Model {

    public class TestEntities {
        public virtual DbSet<Order> Orders { get; set; }
    }
}
