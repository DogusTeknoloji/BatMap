using System;
using System.Collections.Generic;
using System.Linq;
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using Xunit;
using Giver;

namespace BatMap.Tests {

    public class ComplexDataTests {
        private readonly IList<Order> _orders;

        public ComplexDataTests() {
            var random = new Random();

            var products = Give<Product>
                .ToMe(p => p.Supplier = Give<Company>.Single())
                .Now(15);

            _orders = Give<Order>
                .ToMe(o => {
                    o.OrderDetails = Give<OrderDetail>
                        .ToMe(od => od.Product = products[random.Next(15)])
                        .Now(3);
                })
                .Now(10);

            _orders[5].OrderDetails[1].Product = products[9];
            _orders[7].OrderDetails[1].Product = products[9];
        }

        [Fact]
        public void Map_Orders() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var dtoList = config.Map<Order, OrderDTO>(_orders).ToList();

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Fact]
        public void Map_Orders_PreserveReferences() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var dtoList = config.Map<Order, OrderDTO>(_orders, true).ToList();

            Assert.Equal(
                dtoList[5].OrderDetails.ToList()[1].Product,
                dtoList[7].OrderDetails.ToList()[1].Product
            );
        }

        [Fact]
        public void Map_Orders_PreserveReferences_2() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var order = Give<Order>.Single();
            var orderDetail = Give<OrderDetail>.Single();

            order.OrderDetails = new List<OrderDetail> { orderDetail };
            orderDetail.Order = order;

            var orderDto = config.Map<OrderDTO>(order, true);

            Assert.Equal(orderDto, orderDto.OrderDetails.First().Order);
        }

        [Fact]
        public void Map_Orders_With_SkipMember() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>(b => {
                b.SkipMember(o => o.Price);
            });

            var order = Give<Order>.Single();
            var orderDto = config.Map<OrderDTO>(order);

            Assert.NotEqual(order.Price, orderDto.Price);
        }

        [Fact]
        public void Map_Orders_With_MapMember() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>(b => {
                b.MapMember(o => o.Price, (o, mc) => o.Price * 3);
            });

            var order = Give<Order>.Single();
            var orderDto = config.Map<OrderDTO>(order);

            Assert.True(orderDto.Price.Equals(order.Price * 3));
        }

        [Fact]
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

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].SubPrice,
                _orders[3].OrderDetails[2].UnitPrice * _orders[3].OrderDetails[2].Count
            );

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Fact]
        public void Map_Order_To_Existing() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>();
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();
            config.RegisterMap<Company, CompanyDTO>();

            var entity = _orders.First();
            var dto = new OrderDTO();
            var mapDto = config.MapTo(entity, dto);

            Assert.Same(dto, mapDto);
            Assert.Equal(dto.OrderDetails.Count, entity.OrderDetails.Count);
        }

        [Fact]
        public void Project_Orders_With_Navigations() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);
            config.RegisterMap<OrderDetail, OrderDetailDTO>(b => b.SkipMember(od => od.Order));
            config.RegisterMap<Product, ProductDTO>(b => b.SkipMember(p => p.Supplier));

            var projector = config.GetProjector<Order, OrderDTO>();
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Id,
                _orders[3].OrderDetails[2].Product.Id
            );
        }

        [Fact]
        public void Project_Orders_Without_Navigations() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>();

            var projector = config.GetProjector<Order, OrderDTO>(false);
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.Null(dtoList[3].OrderDetails);
        }

        [Fact]
        public void Project_Orders_With_IncludePath() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var projector = config.GetProjector<Order, OrderDTO>(new IncludePath("OrderDetails"));
            var func = Helper.CreateProjector(projector);
            var dtoList = _orders.Select(func).ToList();

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Id,
                _orders[3].OrderDetails[2].Id
            );

            Assert.Null(dtoList[3].OrderDetails.ToList()[2].Product);
        }

        [Fact]
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

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].SubPrice,
                _orders[3].OrderDetails[2].UnitPrice * _orders[3].OrderDetails[2].Count
            );

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Fact]
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

            Assert.Equal(
                dtoList[3].Price,
                _orders[3].OrderDetails.Sum(od => od.UnitPrice)
            );

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Fact]
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
