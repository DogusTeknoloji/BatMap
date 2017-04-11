using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FizzWare.NBuilder;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;

namespace BatMap.Tests {

    [TestClass]
    public class StaticTests {
        public IList<Customer> Customers;

        [TestInitialize]
        public void Initialize() {
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

        [TestMethod]
        public void Register() {
            Mapper.RegisterMap<Customer, CustomerDTO>(b => {
                b.SkipMember(c => c.Endorsement);
            });
            Mapper.RegisterMap(typeof(City), typeof(CityDTO));
            Mapper.RegisterMap<City, CityDTO>((c, mc) => new CityDTO { Id = c.Id, Name = c.Name, Population = c.Population });
        }

        [TestMethod]
        public void MapTwoGeneric() {
            var entity = Customers[0];
            var dto = Mapper.Map<Customer, CustomerDTO>(entity);

            Assert.AreEqual(entity.Id, dto.Id);
        }

        [TestMethod]
        public void MapGeneric() {
            var entity = Customers[0];
            var dto = Mapper.Map<CustomerDTO>(entity, true);

            Assert.AreEqual(entity.Id, dto.Id);
        }

        [TestMethod]
        public void MapWithoutDestination() {
            var entity = Customers[0];
            var dto = Mapper.Map(entity);

            Assert.IsInstanceOfType(dto, typeof(CustomerDTO));
        }

        [TestMethod]
        public void MapWithDestination() {
            var entity = Customers[0];
            var dto = Mapper.Map(entity, typeof(CustomerDTO));

            Assert.IsInstanceOfType(dto, typeof(CustomerDTO));
        }

        [TestMethod]
        public void MapEnumerable() {
            var dtos = Customers.Map<Customer, CustomerDTO>();

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void MapDictionary() {
            var dict = Customers.ToDictionary(c => c.Id, c => c);
            var dtoDict = Mapper.Map<int, Customer, int, CustomerDTO>(dict);

            Assert.IsTrue(dtoDict.All(kvp => kvp.Key == kvp.Value.Id));
        }

        [TestMethod]
        public void ProjectTo() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(false);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void ProjectToWithExpression() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(c => c.Addresses);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void ProjectToWithInclude() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(new IncludePath("Addresses"));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }
    }
}
