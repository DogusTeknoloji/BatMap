using System;
using System.Collections.Generic;
using System.Linq;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace BatMap.Tests {

    [TestFixture]
    public class ConfigurationTests {

        [Test]
        public void Unregistered_Generic_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() => new MapConfiguration().Map<CustomerDTO>(new Customer()));
        }

        [Test]
        public void Unregistered_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() => new MapConfiguration().Map(new Customer()));
        }

        [Test]
        public void Register_With_Non_Member_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() =>
                new MapConfiguration().RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.Addresses.Count > 0, (c, mc) => c.CompanyName.Length > 0))
            );
        }

        [Test]
        public void Register_With_Not_Owned_Member_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() =>
                new MapConfiguration().RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.Address.Detail, (c, mc) => c.CompanyName))
            );
        }

        [Test]
        public void Register_With_Invalid_Member_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() =>
                new MapConfiguration().RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.OrderCount, (c, mc) => c.CompanyName.Length))
            );
        }

        [Test]
        public void Register_Can_Access_Projector_Via_Interface() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>(b => {
                var builder = b as IMapBuilder<MapBuilder<Customer, CustomerDTO>, Customer, CustomerDTO>;
                Assert.IsNotNull(builder);
                Assert.IsNotNull(builder.GetProjector());
            });
        }

        [Test]
        public void Register_With_Non_MemberInitExpression() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>((c, mc) => null);
            Assert.IsNull(config.Map<CustomerDTO>(new Customer(), true));
        }

        [Test]
        public void Register_Single_To_Many_Throws_Exception() {
            Assert.Throws<ArrayTypeMismatchException>(() => new MapConfiguration().RegisterMap<ForTest1, ForTest1DTO>());
        }

        [Test]
        public void Register_Many_To_Single_Throws_Exception() {
            Assert.Throws<ArrayTypeMismatchException>(() => new MapConfiguration().RegisterMap<ForTest1DTO, ForTest1>());
        }

        [Test]
        public void Register_With_Type_Cast() {
            var config = new MapConfiguration();
            config.RegisterMap<ForTest2, ForTest2DTO>();

            var dto = config.Map<ForTest2DTO>(new ForTest2 { Number = 5 });

            Assert.AreEqual(dto.Number, 5);
        }

        [Test]
        public void Map_With_Empty_Collection() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            var customer = new Customer();
            var dto = config.Map<CustomerDTO>(customer);

            Assert.AreEqual(dto.Orders.Count, 0);
        }

        [Test]
        public void Map_With_NonList() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            var customers = Builder<Customer>.CreateListOfSize(5).Build().Select(c => c);
            var dtos = config.Map<Customer, CustomerDTO>(customers);

            Assert.AreEqual(dtos.Count(), 5);
        }

        [Test]
        public void Map_Array_Property() {
            var entities = Builder<ForTest3>
                .CreateListOfSize(5)
                .All()
                .Do(c => {
                    c.Cities = Builder<City>.CreateListOfSize(10).Build().ToArray();
                })
                .Build();
            entities[4].Cities = null;

            var config = new MapConfiguration();
            config.RegisterMap<ForTest3, ForTest3DTO>();
            config.RegisterMap<City, CityDTO>();
            var dtos = config.Map<ForTest3, ForTest3DTO>(entities).ToList();

            Assert.True(dtos[3].Cities[2].Name == entities[3].Cities[2].Name);
            Assert.IsNull(dtos[4].Cities);
        }

        [Test]
        public void Register_With_Dictionary() {
            var config = new MapConfiguration();
            config.RegisterMap<ForTest4, ForTest4DTO>();
            config.RegisterMap<Order, OrderDTO>();

            var entity = new ForTest4 {
                Orders = new Dictionary<int, Order> {
                    {1, new Order { Id = 1 } },
                    {2, new Order { Id = 2 } }
                }
            };
            var dto = config.Map<ForTest4DTO>(entity);

            Assert.AreEqual(dto.Orders[2].Id, 2);
        }

        [Test]
        public void Map_Null_Dictionary_Returns_Null() {
            var config = new MapConfiguration();

            Assert.IsNull(config.Map<int, Customer, int, CustomerDTO>(null));
        }

        [Test]
        public void Map_Empty_Dictionary_Returns_Empty() {
            var config = new MapConfiguration();

            var dict = new Dictionary<int, Customer>();
            Assert.AreEqual(config.Map<int, Customer, int, CustomerDTO>(dict).Count, 0);
        }

        [Test]
        public void Map_Dictionary_With_Class_Key() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            var customer = Builder<Customer>.CreateNew().Build();
            var dict = new Dictionary<Customer, int> { { customer, customer.Id } };

            var dtoDict = config.Map<Customer, int, CustomerDTO, int>(dict);

            Assert.AreEqual(customer.Id, dtoDict.First().Key.Id);
        }

        [Test]
        public void Map_With_Preserve_References() {
            var config = new MapConfiguration();
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();

            var orderDetail1 = Builder<OrderDetail>.CreateNew().Build();
            var orderDetail2 = Builder<OrderDetail>.CreateNew().Build();
            var product = Builder<Product>.CreateNew().Build();
            orderDetail1.Product = product;
            orderDetail2.Product = product;

            var orderDetails = new List<OrderDetail> { orderDetail1, orderDetail2 };
            var dtos = config.Map<OrderDetail, OrderDetailDTO>(orderDetails, true).ToList();

            Assert.AreEqual(dtos[0].Product, dtos[1].Product);
        }

        [Test]
        public void Map_Two_Generic_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.Address, (c, mc) => mc.Map<Address, AddressDTO>(c.MainAddress)));

            Assert.IsNull(config.Map<Customer, CustomerDTO>((Customer)null));
            Assert.IsNull(config.Map<Customer, CustomerDTO>(new Customer()).Address);
        }

        [Test]
        public void Map_Generic_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.IsNull(config.Map<CustomerDTO>(null));
        }

        [Test]
        public void Map_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.IsNull(config.Map(null, typeof(CustomerDTO)));
        }

        [Test]
        public void Null_Map_Without_Destination_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.IsNull(config.Map(null, true));
        }

        [Test]
        public void Map_Without_Destination_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            config.Map(new Customer(), null);
        }

        [Test]
        public void Map_Enumerable_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.IsNull(config.Map<Customer, CustomerDTO>((IEnumerable<Customer>)null));
        }
    }
}
