#if NET_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif

namespace BatMap.Tests.Model {

    public class TestEntities : DbContext {
#if NET_CORE
        private static readonly DbContextOptions<TestEntities> _options;

        static TestEntities() {
            _options = new DbContextOptionsBuilder<TestEntities>()
                .UseInMemoryDatabase(databaseName: "BatMap_Test")
                .Options;
        }

        public TestEntities(DbContextOptions<TestEntities> options): base(options) {
        }
#else

        static TestEntities() {
            Database.SetInitializer<TestEntities>(null);
        }

        TestEntities(): base("Server=(localdb)\\mssqllocaldb") {
        }
#endif

        public static TestEntities Create() {
#if NET_CORE
            return new TestEntities(_options);
#else
            return new TestEntities();
#endif
        }

        [ExcludeFromCodeCoverage]
        public virtual DbSet<Order> Orders { get; set; }
    }
}
