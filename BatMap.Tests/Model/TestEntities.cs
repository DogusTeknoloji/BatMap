using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;

namespace BatMap.Tests.Model {

    public class TestEntities {
        [ExcludeFromCodeCoverage]
        public virtual DbSet<Order> Orders { get; set; }
    }
}
