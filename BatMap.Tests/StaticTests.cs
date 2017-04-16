using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
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
        public void Enumerable_ProjectTo() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(false);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void Enumerable_ProjectToWithExpression() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(c => c.Addresses);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void Enumerable_ProjectToWithInclude() {
            var dtos = Customers.ProjectTo<Customer, CustomerDTO>(new IncludePath("Addresses"));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void Queryable_ProjectTo() {
            var dtos = Customers.AsQueryable().ProjectTo<CustomerDTO>(true);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void Queryable_ProjectToWithExpression() {
            var dtos = Customers.AsQueryable().ProjectTo<Customer, CustomerDTO>(c => c.Addresses);

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }


        [Test]
        public void Queryable_ProjectToWithInclude() {
            var dtos = Customers.AsQueryable().ProjectTo<CustomerDTO>(new IncludePath("Addresses"));

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void GetProjector() {
            var projector = Mapper.GetProjector<Customer, CustomerDTO>(false);
            var dtos = Customers.Select(projector.Compile());

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }

        [Test]
        public void GetProjector_WithExpression() {
            var projector = Mapper.GetProjector<Customer, CustomerDTO>(c => c.Addresses);
            var dtos = Customers.Select(projector.Compile());

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }


        [Test]
        public void GetProjector_WithInclude() {
            var projector = Mapper.GetProjector<Customer, CustomerDTO>(new IncludePath("Addresses"));
            var dtos = Customers.Select(projector.Compile());

            Assert.AreEqual(dtos.Count(), Customers.Count);
        }
    }
}
