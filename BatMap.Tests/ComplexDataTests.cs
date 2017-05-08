using System;
using System.Collections.Generic;
using System.Linq;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace BatMap.Tests {

    [TestFixture]
    public class ComplexDataTests {
        private readonly IList<Order> _orders;

        public ComplexDataTests() {
            var random = new Random();

            var products = Builder<Product>
                .CreateListOfSize(15)
                .All()
                .Do(p => p.Supplier = Builder<Company>.CreateNew().Build())
                .Build();

            _orders = Builder<Order>
                .CreateListOfSize(10)
                .All()
                .Do(o => {
                    o.OrderDetails = Builder<OrderDetail>
                        .CreateListOfSize(3)
                        .All()
                        .Do(od => od.Product = products[random.Next(15)])
                        .Build();
                })
                .Build();

            _orders[5].OrderDetails[1].Product = products[9];
            _orders[7].OrderDetails[1].Product = products[9];
        }

        [Test]
        public void Map_Orders() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var dtoList = config.Map<Order, OrderDTO>(_orders).ToList();

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Test]
        public void Map_Orders_PreserveReferences() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var dtoList = config.Map<Order, OrderDTO>(_orders, true).ToList();

            Assert.AreEqual(
                dtoList[5].OrderDetails.ToList()[1].Product,
                dtoList[7].OrderDetails.ToList()[1].Product
            );
        }

        [Test]
        public void Map_Orders_PreserveReferences_2() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var order = Builder<Order>.CreateNew().Build();
            var orderDetail = Builder<OrderDetail>.CreateNew().Build();

            order.OrderDetails = new List<OrderDetail> { orderDetail };
            orderDetail.Order = order;

            var orderDto = config.Map<OrderDTO>(order, true);

            Assert.AreEqual(orderDto, orderDto.OrderDetails.First().Order);
        }

        [Test]
        public void Map_Orders_With_SkipMember() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>(b => {
                b.SkipMember(o => o.Price);
            });

            var order = Builder<Order>.CreateNew().Build();
            var orderDto = config.Map<OrderDTO>(order);

            Assert.AreNotEqual(order.Price, orderDto.Price);
        }

        [Test]
        public void Map_Orders_With_MapMember() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>(b => {
                b.MapMember(o => o.Price, (o, mc) => o.Price * 3);
            });

            var order = Builder<Order>.CreateNew().Build();
            var orderDto = config.Map<OrderDTO>(order);

            Assert.IsTrue(orderDto.Price.Equals(order.Price * 3));
        }

        [Test]
        public void Map_Orders_Custom_Expression() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>((o, mc) => new OrderDTO {
                Id = o.Id,
                OrderNo = o.OrderNo,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO {
                    Id = od.Id,
                    Product = mc.Map<Product, ProductDTO>(od.Product),
                    SubPrice = od.UnitPrice * od.Count
                }).ToList(),
                Price = Math.Round(o.Price)
            });
            config.RegisterMap<Product, ProductDTO>();
            config.RegisterMap<Company, CompanyDTO>();

            var dtoList = config.Map<Order, OrderDTO>(_orders).ToList();

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].SubPrice,
                _orders[3].OrderDetails[2].UnitPrice * _orders[3].OrderDetails[2].Count
            );

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Test]
        public void Map_Order_To_Existing() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>();
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();
            config.RegisterMap<Company, CompanyDTO>();

            var entity = _orders.First();
            var dto = new OrderDTO();
            var mapDto = config.MapTo(entity, dto);

            Assert.AreSame(dto, mapDto);
            Assert.AreEqual(dto.OrderDetails.Count, entity.OrderDetails.Count);
        }

        [Test]
        public void Project_Orders_With_Navigations() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);
            config.RegisterMap<OrderDetail, OrderDetailDTO>(b => b.SkipMember(od => od.Order));
            config.RegisterMap<Product, ProductDTO>(b => b.SkipMember(p => p.Supplier));

            var projector = config.GetProjector<Order, OrderDTO>();
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].Product.Id,
                _orders[3].OrderDetails[2].Product.Id
            );
        }

        [Test]
        public void Project_Orders_Without_Navigations() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>();

            var projector = config.GetProjector<Order, OrderDTO>(false);
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.IsNull(dtoList[3].OrderDetails);
        }

        [Test]
        public void Project_Orders_With_IncludePath() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var projector = config.GetProjector<Order, OrderDTO>(new IncludePath("OrderDetails"));
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].Id,
                _orders[3].OrderDetails[2].Id
            );

            Assert.IsNull(dtoList[3].OrderDetails.ToList()[2].Product);
        }

        [Test]
        public void Project_Orders_Custom_Expression() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>((o, mc) => new OrderDTO {
                Id = o.Id,
                OrderNo = o.OrderNo,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO {
                    Id = od.Id,
                    Product = mc.Map<Product, ProductDTO>(od.Product),
                    SubPrice = od.UnitPrice * od.Count
                }).ToList(),
                Price = o.Price
            });
            config.RegisterMap<Product, ProductDTO>();
            config.RegisterMap<Company, CompanyDTO>();

            var projector = config.GetProjector<Order, OrderDTO>(o => o.OrderDetails.Select(od => od.Product.Supplier));
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].SubPrice,
                _orders[3].OrderDetails[2].UnitPrice * _orders[3].OrderDetails[2].Count
            );

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Test]
        public void Project_Orders_Custom_Expression_2() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>((o, mc) => new OrderDTO {
                Id = o.OrderDetails.ToList().Select(od => od.OrderId).First(),
                OrderNo = o.OrderNo,
                Price = o.OrderDetails.Sum(od => od.UnitPrice),
                OrderDetails = mc.MapToList<OrderDetail, OrderDetailDTO>(o.OrderDetails)
            });
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();
            config.RegisterMap<Company, CompanyDTO>();

            var projector = config.GetProjector<Order, OrderDTO>(o => o.OrderDetails.Select(od => od.Product.Supplier));
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.AreEqual(
                dtoList[3].Price,
                _orders[3].OrderDetails.Sum(od => od.UnitPrice)
            );

            Assert.AreEqual(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Test]
        public void Project_Invalid_Method_Throws_Exception() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>((o, mc) => new OrderDTO {
                Id = o.Id,
                OrderNo = o.OrderNo,
                Price = mc.MapToDictionary<int, OrderDetail, int, int>(o.OrderDetails.ToDictionary(od => od.Id, od => od)).Sum(x => x.Key)
            });
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();
            config.RegisterMap<Company, CompanyDTO>();

            Assert.Throws<InvalidOperationException>(() => config.GetProjector<Order, OrderDTO>(o => o.OrderDetails.Select(od => od.Product.Supplier)));
        }
    }
}
