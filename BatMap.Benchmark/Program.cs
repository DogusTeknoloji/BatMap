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

            AutoMapper.Mapper.Initialize(cfg => {
                cfg.CreateMap<Customer, CustomerDTO>();
                cfg.CreateMap<Address, AddressDTO>();
                cfg.CreateMap<City, CityDTO>();
                cfg.CreateMap<Order, OrderDTO>();
                cfg.CreateMap<OrderDetail, OrderDetailDTO>();
            });

            ExpressMapper.Mapper.Register<Customer, CustomerDTO>();
            ExpressMapper.Mapper.Register<Address, AddressDTO>();
            ExpressMapper.Mapper.Register<City, CityDTO>();
            ExpressMapper.Mapper.Register<Order, OrderDTO>();
            ExpressMapper.Mapper.Register<OrderDetail, OrderDetailDTO>();

            Nelibur.ObjectMapper.TinyMapper.Bind<Customer, CustomerDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<Address, AddressDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<City, CityDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<Order, OrderDTO>();
            Nelibur.ObjectMapper.TinyMapper.Bind<OrderDetail, OrderDetailDTO>();

            var p = new Program();
            p.ByHand();
            p.Bat_Map();
            p.Auto_Map();
            p.Express_Map();
            p.Fast_Map();
            p.Mapster_Map();
            p.Safe_Map();
            p.Tiny_Map();
        }

        static void Main(string[] args) {
            var summary = BenchmarkRunner.Run<Program>();
        }

        [Benchmark]
        public void ByHand() {
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
        public void Bat_Map() {
            var customerDTOs = _customers.Map<Customer, CustomerDTO>().ToList();
        }

        [Benchmark]
        public void Auto_Map() {
            var customerDTOs = _customers.Select(c => AutoMapper.Mapper.Map<CustomerDTO>(c)).ToList();
        }

        [Benchmark]
        public void Express_Map() {
            var customerDTOs = _customers.Select(c => ExpressMapper.Mapper.Map<Customer, CustomerDTO>(c)).ToList();
        }

        [Benchmark]
        public void Fast_Map() {
            var customerDTOs = FastMapper.TypeAdapter.Adapt<IList<Customer>, List<CustomerDTO>>(_customers);
        }

        [Benchmark]
        public void Mapster_Map() {
            var customerDTOs = Mapster.TypeAdapter.Adapt<IList<Customer>, List<CustomerDTO>>(_customers);
        }

        [Benchmark]
        public void Safe_Map() {
            var customerDTOs = _customers.Select(c => SafeMapper.SafeMap.Convert<Customer, CustomerDTO>(c)).ToList();
        }

        [Benchmark]
        public void Tiny_Map() {
            var customerDTOs = _customers.Select(c => Nelibur.ObjectMapper.TinyMapper.Map<Customer, CustomerDTO>(c)).ToList();
        }
    }
}
