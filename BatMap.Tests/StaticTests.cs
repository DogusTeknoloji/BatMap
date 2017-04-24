using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace BatMap.Tests {

    /// <summary>
    /// Tests only static API method signatures.
    /// </summary>
    [TestFixture]
    public class StaticTests {
        public IList<Customer> Customers;

        public StaticTests() {
            Customers = Builder<Customer>
                .CreateListOfSize(5)
                .All()
                .Do(c => {
                    var addresses = Builder<Address>
                        .CreateListOfSize(3)
                        .All()
                        .Do(a => a.City = Builder<City>.CreateNew().Build())
                        .Build();

                    c.Addresses = addresses;
                    c.MainAddress = addresses[0];
                })
                .Build();
        }

        [Test]
        public void Static_Availability_Test() {
            // check if all instance methods are also available as static API and static API does not have any extra method
            var skipMethods = new[] { "GetProjector", "GetMapDefinition", "GenerateMapDefinition" };
            var instanceMethods = typeof(MapConfiguration).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => !skipMethods.Contains(m.Name) && m.DeclaringType != typeof(object))
                .ToList();
            var staticMethods = typeof(Mapper).GetMethods(BindingFlags.Static | BindingFlags.Public).ToList();
            
            Assert.IsTrue(instanceMethods.Count == staticMethods.Count 
                && instanceMethods.All(m => staticMethods.Any(sm =>
                    m.Name == sm.Name && m.ToString() == sm.ToString()
                ))
            );
        }

        [Test]
        public void Register() {
            Mapper.RegisterMap<Customer, CustomerDTO>(b => {
                b.SkipMember(c => c.Endorsement);
            });
        }

        [Test]
        public void RegisterWithType() {
            Mapper.RegisterMap(typeof(City), typeof(CityDTO));
        }

        [Test]
        public void RegisterWithExpression() {
            Mapper.RegisterMap<City, CityDTO>((c, mc) => new CityDTO { Id = c.Id, Name = c.Name, Population = c.Population });
        }

        [Test]
        public void MapTwoGeneric() {
            var entity = Customers[0];
            var dto = Mapper.Map<Customer, CustomerDTO>(entity);

            Assert.AreEqual(entity.Id, dto.Id);
        }

        [Test]
        public void MapGeneric() {
            var entity = Customers[0];
            var dto = Mapper.Map<CustomerDTO>(entity, true);

            Assert.AreEqual(entity.Id, dto.Id);
        }

        [Test]
        public void MapWithoutDestination() {
            var entity = Customers[0];
            var dto = Mapper.Map(entity);

            Assert.IsInstanceOf(typeof(CustomerDTO), dto);
        }

        [Test]
        public void MapWithDestination() {
            var entity = Customers[0];
            var dto = Mapper.Map(entity, typeof(CustomerDTO));

            Assert.IsInstanceOf(typeof(CustomerDTO), dto);
        }

        [Test]
        public void MapEnumerable() {
            var dtos = Customers.Map<Customer, CustomerDTO>();

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void MapDictionary() {
            var dict = Customers.ToDictionary(c => c.Id, c => c);
            var dtoDict = Mapper.Map<int, Customer, int, CustomerDTO>(dict);

            Assert.IsTrue(dtoDict.All(kvp => kvp.Key == kvp.Value.Id));
        }
       
        [Test]
        public void Queryable_ProjectTo() {
            var dtos = Customers.AsQueryable().ProjectTo<CustomerDTO>(true);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void Queryable_ProjectToWithExpression() {
            var dtos = Customers.AsQueryable().ProjectTo<Customer, CustomerDTO>(c => c.Addresses.Select(a => a.City));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void Queryable_ProjectToWithInclude() {
            var dtos = Customers.AsQueryable().ProjectTo<CustomerDTO>(new IncludePath("Addresses"));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }
    }
}
