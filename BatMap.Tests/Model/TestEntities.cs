using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;

namespace BatMap.Tests.Model {

    public class TestEntities: DbContext {
        [ExcludeFromCodeCoverage]
        public virtual DbSet<Order> Orders { get; set; }
    }
}
