using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FizzWare.NBuilder;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;

namespace BatMap.Tests {

    /// <summary>
    /// Tests only static API method signatures.
    /// </summary>
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
        }

        [TestMethod]
        public void RegisterWithType() {
            Mapper.RegisterMap(typeof(City), typeof(CityDTO));
        }

        [TestMethod]
        public void RegisterWithExpression() {
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
        public void Enumerable_ProjectTo() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(false);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void Enumerable_ProjectToWithExpression() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(c => c.Addresses);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void Enumerable_ProjectToWithInclude() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(new IncludePath("Addresses"));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void Queryable_ProjectTo() {
            var dtos = Customers.AsQueryable().ProjectTo<CustomerDTO>(true);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void Queryable_ProjectToWithExpression() {
            var dtos = Customers.AsQueryable().ProjectTo<Customer, CustomerDTO>(c => c.Addresses);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }


        [TestMethod]
        public void Queryable_ProjectToWithInclude() {
            var dtos = Customers.AsQueryable().ProjectTo<CustomerDTO>(new IncludePath("Addresses"));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void GetProjector() {
            var projector = Mapper.GetProjector<Customer, CustomerDTO>(false);
            var dtos = Customers.Select(projector.Compile());

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [TestMethod]
        public void GetProjector_WithExpression() {
            var projector = Mapper.GetProjector<Customer, CustomerDTO>(c => c.Addresses);
            var dtos = Customers.Select(projector.Compile());

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }


        [TestMethod]
        public void GetProjector_WithInclude() {
            var projector = Mapper.GetProjector<Customer, CustomerDTO>(new IncludePath("Addresses"));
            var dtos = Customers.Select(projector.Compile());

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }
    }
}
