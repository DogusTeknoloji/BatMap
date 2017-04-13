using System;
using System.Collections.Generic;
using System.Linq;
using BatMap.Benchmark.DTO;
using BatMap.Benchmark.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FizzWare.NBuilder;

namespace BatMap.Benchmark {

    public class Program {
        static IList<Customer> _customers;

        static Program() {
            var rnd = new Random();
            _customers = Builder<Customer>
                .CreateListOfSize(1000)
                .All()
                .Do(c => {
                    c.Addresses = Builder<Address>
                        .CreateListOfSize(rnd.Next(3) + 1)
                        .All()
                        .Do(a => a.City = Builder<City>.CreateNew().Build())
                        .Build()
                        .ToList();
                    c.Orders = Builder<Order>
                        .CreateListOfSize(rnd.Next(10) + 1)
                        .All()
                        .Do(o => {
                            o.OrderDetails = Builder<OrderDetail>
                                .CreateListOfSize(rnd.Next(5) + 1)
                                .Build()
                                .ToList();
                        })
                        .Build()
                        .ToList();
                })
                .Build();

            Mapper.RegisterMap<Customer, CustomerDTO>();
            Mapper.RegisterMap<Address, AddressDTO>();
            Mapper.RegisterMap<City, CityDTO>();
            Mapper.RegisterMap<Order, OrderDTO>();
            Mapper.RegisterMap<OrderDetail, OrderDetailDTO>();

            global::AutoMapper.Mapper.Initialize(cfg => {
                cfg.CreateMap<Customer, CustomerDTO>();
                cfg.CreateMap<Address, AddressDTO>();
                cfg.CreateMap<City, CityDTO>();
                cfg.CreateMap<Order, OrderDTO>();
                cfg.CreateMap<OrderDetail, OrderDetailDTO>();
            });

            global::ExpressMapper.Mapper.Register<Customer, CustomerDTO>();
            global::ExpressMapper.Mapper.Register<Address, AddressDTO>();
            global::ExpressMapper.Mapper.Register<City, CityDTO>();
            global::ExpressMapper.Mapper.Register<Order, OrderDTO>();
            global::ExpressMapper.Mapper.Register<OrderDetail, OrderDetailDTO>();

            Nelibur.ObjectMapper.TinyMapper.Bind<Customer, CustomerDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<Address, AddressDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<City, CityDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<Order, OrderDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<OrderDetail, OrderDetailDTO>();
        }

        static void Main(string[] args) {
            var summary = BenchmarkRunner.Run<Program>();
            //ManualTest();
        }

        /// <summary>
        /// Used for benchmarking via [Visual Studio -> Analyze -> Performance Profiler]
        /// </summary>
        public static void ManualTest() {
            var p = new Program();
            p.HandWritten();
            p.BatMap();
            p.AutoMapper();
            p.ExpressMapper();
            p.FastMapper();
            p.Mapster();
            p.SafeMapper();
            p.TinyMapper();
        }

        [Benchmark]
        public void HandWritten() {
            var customerDTOs = _customers.Select(c => new CustomerDTO {
                Id = c.Id,
                Addresses = c.Addresses.Select(a => new AddressDTO {
                    City = new CityDTO {
                        Id = a.City.Id,
                        Name = a.City.Name,
                        Population = a.City.Population
                    },
                    Detail = a.Detail,
                    Id = a.Id
                }).ToList(),
                CompanyName = c.CompanyName,
                Endorsement = c.Endorsement,
                Orders = c.Orders.Select(o => new OrderDTO {
                    Id = o.Id,
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailDTO {
                        Id = od.Id,
                        Count = od.Count,
                        UnitPrice = od.UnitPrice
                    }).ToList(),
                    OrderNo = o.OrderNo,
                    Price = o.Price
                }).ToList(),
                Phone = c.Phone
            }).ToList();
        }

        [Benchmark]
        public void BatMap() {
            var customerDTOs = _customers.Map<Customer, CustomerDTO>().ToList();
        }

        [Benchmark]
        public void AutoMapper() {
            var customerDTOs = _customers.Select(c => global::AutoMapper.Mapper.Map<CustomerDTO>(c)).ToList();
        }

        [Benchmark]
        public void ExpressMapper() {
            var customerDTOs = _customers.Select(c => global::ExpressMapper.Mapper.Map<Customer, CustomerDTO>(c)).ToList();
        }

        [Benchmark]
        public void FastMapper() {
            var customerDTOs = global::FastMapper.TypeAdapter.Adapt<IList<Customer>, List<CustomerDTO>>(_customers);
        }

        [Benchmark]
        public void Mapster() {
            var customerDTOs = global::Mapster.TypeAdapter.Adapt<IList<Customer>, List<CustomerDTO>>(_customers);
        }

        [Benchmark]
        public void SafeMapper() {
            var customerDTOs = _customers.Select(c => global::SafeMapper.SafeMap.Convert<Customer, CustomerDTO>(c)).ToList();
        }

        [Benchmark]
        public void TinyMapper() {
            var customerDTOs = _customers.Select(c => Nelibur.ObjectMapper.TinyMapper.Map<Customer, CustomerDTO>(c)).ToList();
        }
    }
}
