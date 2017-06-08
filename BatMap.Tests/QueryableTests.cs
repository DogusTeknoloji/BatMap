using System;
using System.Collections.Generic;
using System.Linq;
#if NET_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif
using BatMap.Tests.DTO;
using BatMap.Tests.Model;
using Xunit;
using Giver;

namespace BatMap.Tests {

    public class QueryableTests {
        private readonly IList<Order> _orders;

        public QueryableTests() {
            var random = new Random();

            var products = Give<Product>
                .ToMe(p => p.Supplier = Give<Company>.ToMe(s => s.Addresses = Give<Address>.Many(2)).Now())
                .Now(15);

            _orders = Give<Order>
                .ToMe(o => {
                    o.OrderDetails = Give<OrderDetail>
                        .ToMe(od => od.Product = products[random.Next(15)])
                        .Now(3);
                })
                .Now(10);
        }

        [Fact]
        public void Get_Includes() {
            var context = TestEntities.Create();
            var q = context.Orders
                .Include(o => o.OrderDetails.Select(od => od.Product.Supplier.Addresses.Select(a => a.City)))
                .Include(o => o.OrderDetails.Select(od => od.Product.Supplier.MainAddress));

            var oIncludes = Helper.GetIncludes(q).FirstOrDefault();
            Assert.True(oIncludes != null && oIncludes.Member == "OrderDetails");

            var odInclude = oIncludes.Children.FirstOrDefault();
            Assert.True(odInclude != null && odInclude.Member == "Product");

            var pInclude = odInclude.Children.FirstOrDefault();
            Assert.True(pInclude != null && pInclude.Member == "Supplier");

            Assert.Equal(pInclude.Children.Count(), 2);
        }

        [Fact]
        public void Project_Orders() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>();
            config.RegisterMap<OrderDetail, OrderDetailDTO>();
            config.RegisterMap<Product, ProductDTO>();

            var query = _orders.AsQueryable();
            var dtoQuery = config.ProjectTo<OrderDTO>(query);
            var dtoList = dtoQuery.ToList();

            Assert.Null(dtoList[3].OrderDetails);
        }

        [Fact]
        public void Project_Orders_With_Details_Product_Company() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var query = _orders.AsQueryable();
            var dtoQuery = config.ProjectTo<Order, OrderDTO>(query, o => o.OrderDetails.Select(od => od.Product.Supplier));
            var dtoList = dtoQuery.ToList();

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Fact]
        public void Project_Orders_With_Details_Product_Company_2() {
            var config = new MapConfiguration(DynamicMapping.MapAndCache);

            var query = _orders.AsQueryable();
            var dtoQuery = config.ProjectTo<Order, OrderDTO>(query, o => o.OrderDetails.Select(od => od.Product).Select(p => p.Supplier));
            var dtoList = dtoQuery.ToList();

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );
        }

        [Fact]
        public void Project_Orders_Custom_Expression() {
            // we aren't registering OrderDetail-OrderDetailDTO because we declare the custom mapping
            // even we don't include OrderDetail in the query, it will be mapped
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

            var query = _orders.AsQueryable();
            var dtoQuery = config.ProjectTo<OrderDTO>(query, new IncludePath[] { });
            var dtoList = dtoQuery.ToList();

            Assert.NotNull(dtoList[3].OrderDetails);
            Assert.Equal(dtoList[3].OrderDetails.Count, _orders[3].OrderDetails.Count);
            Assert.True(dtoList.All(o => o.OrderDetails.All(od => od.Product == null)));
        }

        [Fact]
        public void Project_Orders_Custom_Expression2() {
            var config = new MapConfiguration();
            config.RegisterMap<Order, OrderDTO>((o, mc) => new OrderDTO {
                Id = o.Id,
                OrderNo = o.OrderNo,
                // ReSharper disable once ConvertClosureToMethodGroup
                // it will fail for method group because it must be an "Expression" to be handled
                // mc.Map will be included in the final expression because we are using "Select" for "OrderDetails" navigation
                // using "Select" is equal of saying "always map this property my way"
                // if we want it to be dynamically decided for plural navigations we should have used: "mc.MapToList<OrderDetail, OrderDetailDTO>"
                OrderDetails = o.OrderDetails.Select(od => mc.Map<OrderDetail, OrderDetailDTO>(od)).ToList(),
                Price = o.Price
            });
            // similar custom mapping. but this time we need to register OrderDetail-OrderDetailDTO to allow MapContext to do the mapping
            config.RegisterMap<OrderDetail, OrderDetailDTO>();

            var query = _orders.AsQueryable();
            var dtoQuery = config.ProjectTo<OrderDTO>(query, new IncludePath[] { });
            var dtoList = dtoQuery.ToList();

            Assert.NotNull(dtoList[3].OrderDetails);
            Assert.Equal(dtoList[3].OrderDetails.Count, _orders[3].OrderDetails.Count);
            Assert.True(dtoList.All(o => o.OrderDetails.All(od => od.Product == null)));
        }

        [Fact]
        public void Project_Orders_With_Details_Product_Supplier_Custom_Expression() {
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

            var query = _orders.AsQueryable();
            var dtoQuery = config.ProjectTo<Order, OrderDTO>(query, o => o.OrderDetails.Select(od => od.Product).Select(p => p.Supplier));
            var dtoList = dtoQuery.ToList();

            Assert.Equal(
                dtoList[3].OrderDetails.ToList()[2].Product.Supplier.CompanyName,
                _orders[3].OrderDetails[2].Product.Supplier.CompanyName
            );

            Assert.Null(dtoList[3].OrderDetails.ToList()[2].Product.Supplier.Addresses);
        }
    }
}
