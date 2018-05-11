using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using Xunit;
using Giver;
using System.Collections;

namespace BatMap.Tests {

    public class ConfigurationTests {

        [Fact]
        public void Unregistered_Generic_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() => new MapConfiguration().Map<CustomerDTO>(new Customer()));
        }

        [Fact]
        public void Unregistered_Throws_Exception() {
            Assert.Throws<InvalidOperationException>(() => new MapConfiguration().Map(new Customer()));
        }

        [Fact]
        public void Register_With_Non_Member_Throws_Exception() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.Addresses.Count > 0, (c, mc) => c.CompanyName.Length > 0))
            );
        }

        [Fact]
        public void Register_With_Not_Owned_Member_Throws_Exception() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.Address.Detail, (c, mc) => c.CompanyName))
            );
        }

        [Fact]
        public void Register_With_Invalid_Member_Throws_Exception() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.OrderCount, (c, mc) => c.CompanyName.Length))
            );
        }

        [Fact]
        public void Register_Skip_Not_Owned_Member_String_Throws_Exception() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap(typeof(Customer), typeof(CustomerDTO), b => b.SkipMember("OrderCount"))
            );
        }

        [Fact]
        public void Register_With_Not_Owned_Member_String_Throws_Exception() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap(typeof(Customer), typeof(CustomerDTO), b => b.MapMember("OrderCount", "CompanyName"))
            );
        }

        [Fact]
        public void Register_From_Not_Owned_Member_String_Throws_Exception() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap(typeof(Customer), typeof(CustomerDTO), b => b.MapMember("Endorsement", "Orders.TotalCount"))
            );
        }

        [Fact]
        public void Register_From_Not_Owned_Member_String_Throws_Exception_2() {
            Assert.Throws<ArgumentException>(() =>
                new MapConfiguration().RegisterMap(typeof(Customer), typeof(CustomerDTO), b => b.MapMember("Endorsement", "Totals.OrderCount"))
            );
        }

        [Fact]
        public void Register_Single_To_Many_Throws_Exception() {
            Assert.Throws<ArrayTypeMismatchException>(() => new MapConfiguration().RegisterMap<ForTest1, ForTest1DTO>());
        }

        [Fact]
        public void Register_Many_To_Single_Throws_Exception() {
            Assert.Throws<ArrayTypeMismatchException>(() => new MapConfiguration().RegisterMap<ForTest1DTO, ForTest1>());
        }

        [Fact]
        public void Register_With_Type_Cast() {
            var config = new MapConfiguration();
            config.RegisterMap<ForTest2, ForTest2DTO>();
            config.RegisterMap<ForTest2DTO, ForTest2>();

            var dto = config.Map<ForTest2DTO>(new ForTest2 { Number = "5" });
            Assert.Equal(dto.Number, 5);

            var model = config.Map<ForTest2>(new ForTest2DTO { Number = 5 });
            Assert.Equal(model.Number, "5");
        }

        [Fact]
        public void Map_With_Non_MemberInitExpression() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>((c, mc) => null);
            Assert.Null(config.Map<CustomerDTO>(new Customer(), true));
        }

        [Fact]
        public void Map_Existing_With_Non_MemberInitExpression() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>((c, mc) => mc.Map<Customer, CustomerDTO>(c));
            Assert.Throws<InvalidOperationException>(() => config.MapTo(new Customer(), new CustomerDTO()));
        }

        [Fact]
        public void Map_With_Empty_Collection() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            var customer = new Customer();
            var dto = config.Map<CustomerDTO>(customer);

            Assert.Equal(dto.Orders.Count, 0);
        }

        [Fact]
        public void Map_With_NonList() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            var customers = Give<Customer>.Many(5);
            var dtos = config.Map<Customer, CustomerDTO>(customers);

            Assert.Equal(dtos.Count(), 5);
        }

        [Fact]
        public void Map_Array_Property() {
            var entities = Give<ForTest3>
                .ToMe(c => {
                    c.Cities = Give<City>.Many(10).ToArray();
                })
                .Now(5);
            entities[4].Cities = null;

            var config = new MapConfiguration();
            config.RegisterMap<ForTest3, ForTest3DTO>();
            config.RegisterMap<City, CityDTO>();
            var dtos = config.Map<ForTest3, ForTest3DTO>(entities).ToList();

            Assert.True(dtos[3].Cities[2].Name == entities[3].Cities[2].Name);
            Assert.Null(dtos[4].Cities);
        }

        [Fact]
        public void Map_Dictionary_Property() {
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

            Assert.Equal(dto.Orders[2].Id, 2);
        }

        [Fact]
        public void Map_Collection_Property() {
            var entities = Give<ForTest5>
                .ToMe(c => {
                    c.Cities = new Collection<City>(Give<City>.Many(10));
                })
                .Now(5);
            entities[4].Cities = null;

            var config = new MapConfiguration();
            config.RegisterMap<ForTest5, ForTest5DTO>();
            config.RegisterMap<City, CityDTO>();
            var dtos = config.Map<ForTest5, ForTest5DTO>(entities).ToList();

            Assert.True(dtos[3].Cities[2].Name == entities[3].Cities[2].Name);
            Assert.Null(dtos[4].Cities);
        }

        [Fact]
        public void Map_To_Collection_Property() {
            var entities = Give<ForTest5>
                .ToMe(c => {
                    c.Cities = new Collection<City>(Give<City>.Many(10));
                })
                .Now(5);
            entities[4].Cities = null;

            var config = new MapConfiguration();
            config.RegisterMap<ForTest5, ForTest5DTO>((e, mc) => new ForTest5DTO {
                Cities = mc.MapToCollection<City, CityDTO>(e.Cities)
            });
            config.RegisterMap<City, CityDTO>();
            var dtos = config.Map<ForTest5, ForTest5DTO>(entities).ToList();

            Assert.True(dtos[3].Cities[2].Name == entities[3].Cities[2].Name);
            Assert.Null(dtos[4].Cities);
        }

        [Fact]
        public void Map_HashSet_Property() {
            var entities = Give<ForTest6>
                .ToMe(c => {
                    c.Cities = new HashSet<City>(Give<City>.Many(10));
                })
                .Now(5);
            entities[4].Cities = null;

            var config = new MapConfiguration();
            config.RegisterMap<ForTest6, ForTest6DTO>();
            config.RegisterMap<City, CityDTO>();
            var dtos = config.Map<ForTest6, ForTest6DTO>(entities).ToList();

            Assert.True(dtos[3].Cities.First().Name == entities[3].Cities.First().Name);
            Assert.Null(dtos[4].Cities);
        }

        [Fact]
        public void Map_Null_Returns_Null() {
            var config = new MapConfiguration();

            Assert.Null(config.Map((object)null));
        }

        [Fact]
        public void Map_Null_Dictionary_Returns_Null() {
            var config = new MapConfiguration();

            Assert.Null(config.Map<int, Customer, int, CustomerDTO>(null));
        }

        [Fact]
        public void Map_Empty_Dictionary_Returns_Empty() {
            var config = new MapConfiguration();

            var dict = new Dictionary<int, Customer>();
            Assert.Equal(config.Map<int, Customer, int, CustomerDTO>(dict).Count, 0);
        }

        [Fact]
        public void Map_Dictionary_With_Class_Key() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            var customer = Give<Customer>.Single();
            var dict = new Dictionary<Customer, int> { { customer, customer.Id } };

            var dtoDict = config.Map<Customer, int, CustomerDTO, int>(dict);

            Assert.Equal(customer.Id, dtoDict.First().Key.Id);
        }

        [Fact]
        public void Map_With_Preserve_References() {
            var config = new MapConfiguration();
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();

            var orderDetail1 = Give<OrderDetail>.Single();
            var orderDetail2 = Give<OrderDetail>.Single();
            var product = Give<Product>.Single();
            orderDetail1.Product = product;
            orderDetail2.Product = product;

            var orderDetails = new List<OrderDetail> { orderDetail1, orderDetail2 };
            var dtos = config.Map<OrderDetail, OrderDetailDTO>(orderDetails, true).ToList();

            Assert.Equal(dtos[0].Product, dtos[1].Product);
        }

        [Fact]
        public void Map_Two_Generic_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>(b => b.MapMember(c => c.Address, (c, mc) => mc.Map<Address, AddressDTO>(c.MainAddress)));

            Assert.Null(config.Map<Customer, CustomerDTO>((Customer)null));
            Assert.Null(config.Map<Customer, CustomerDTO>(new Customer()).Address);
        }

        [Fact]
        public void Map_Generic_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.Null(config.Map<CustomerDTO>(null));
        }

        [Fact]
        public void Map_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.Null(config.Map(null, typeof(CustomerDTO)));
        }

        [Fact]
        public void Map_Without_Destination_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.Null(config.Map(null, true));
        }

        [Fact]
        public void Map_Enumerable_With_Null() {
            var config = new MapConfiguration();
            config.RegisterMap<Customer, CustomerDTO>();

            Assert.Null(config.Map<Customer, CustomerDTO>((IEnumerable<Customer>)null));
        }

        [Fact]
        public void Map_Primitive_Enumerable() {
            var config = new MapConfiguration();
            config.RegisterMap<ForTest7, ForTest7DTO>();

            var rnd = new Random();
            var img = Enumerable.Range(0, 100).Select(i => (byte)rnd.Next(255));
            var entity = new ForTest7 {
                Image1 = img.ToArray(),
                Image2 = img.Select(i => (int)i).ToList(),
                Image3 = new HashSet<byte>(img)
            };

            var dto = config.Map<ForTest7DTO>(entity);

            Assert.True(Enumerable.SequenceEqual(entity.Image1.Select(b => (int)b), dto.Image1));
            Assert.True(Enumerable.SequenceEqual(entity.Image2, dto.Image2.Select(b => (int)b)));
            Assert.True(Enumerable.SequenceEqual(entity.Image3, dto.Image3));
        }
    }
}
