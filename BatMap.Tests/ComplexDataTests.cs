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

            var dtoList = config.ProjectTo<Order, OrderDTO>(_orders, o => o.OrderDetails.Select(od => od.Product.Supplier)).ToList();

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

            var dtoList = config.ProjectTo<Order, OrderDTO>(_orders, o => o.OrderDetails.Select(od => od.Product.Supplier)).ToList();

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

            Assert.Throws<InvalidOperationException>(() => {
                var dtoList = config.ProjectTo<Order, OrderDTO>(_orders, o => o.OrderDetails.Select(od => od.Product.Supplier)).ToList();
            });
        }
    }
}
