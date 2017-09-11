using System;
using System.Collections.Generic;
using System.Linq;
using BatMap.Benchmark.DTO;
using BatMap.Benchmark.Model;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Giver;

namespace BatMap.Benchmark {

    public class Program {
        private static readonly IList<Customer> _customers;

        static Program() {
            var rnd = new Random();
            _customers = Give<Customer>.ToMe()
                .With(c => {
                    c.Addresses = Give<Address>.ToMe()
                        .With(a => a.City = Give<City>.Single())
                        .Now(rnd.Next(3) + 1);
                    c.Orders = Give<Order>.ToMe()
                        .With(o => o.OrderDetails = Give<OrderDetail>.Many(rnd.Next(5) + 1))
                        .Now(rnd.Next(10) + 1);
                })
                .Now(1000);

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

#if NET
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
#endif
        }

        static void Main() {
            BenchmarkRunner.Run<Program>();
        }

        [Benchmark]
        public void BatMap() {
            var customerDTOs = _customers.Map<Customer, CustomerDTO>().ToList();
        }

#if NET
        [Benchmark]
        public void HandWritten() {
            var customerDTOs = _customers.Select(c => new CustomerDTO {
                Id = c.Id,
                Addresses = c.Addresses.ConvertAll(a => new AddressDTO {
                    City = new CityDTO {
                        Id = a.City.Id,
                        Name = a.City.Name,
                        Population = a.City.Population
                    },
                    Detail = a.Detail,
                    Id = a.Id
                }),
                CompanyName = c.CompanyName,
                Endorsement = c.Endorsement,
                Orders = c.Orders.ConvertAll(o => new OrderDTO {
                    Id = o.Id,
                    OrderDetails = o.OrderDetails.ConvertAll(od => new OrderDetailDTO {
                        Id = od.Id,
                        Count = od.Count,
                        UnitPrice = od.UnitPrice
                    }),
                    OrderNo = o.OrderNo,
                    Price = o.Price
                }),
                Phone = c.Phone
            }).ToList();
        }
#else
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
#endif

        [Benchmark]
        public void Mapster() {
            var customerDTOs = global::Mapster.TypeAdapter.Adapt<IList<Customer>, List<CustomerDTO>>(_customers);
        }

#if NET
        [Benchmark]
        public void SafeMapper() {
            var customerDTOs = global::SafeMapper.SafeMap.Convert<IList<Customer>, List<CustomerDTO>>(_customers);
        }
#endif

        [Benchmark]
        public void AutoMapper() {
            var customerDTOs = global::AutoMapper.Mapper.Map<IList<Customer>, List<CustomerDTO>>(_customers);
        }

#if NET
        [Benchmark]
        public void TinyMapper() {
            var customerDTOs = Nelibur.ObjectMapper.TinyMapper.Map<IList<Customer>, List<CustomerDTO>>(_customers);
        }

        [Benchmark]
        public void ExpressMapper() {
            var customerDTOs = global::ExpressMapper.Mapper.Map<IList<Customer>, List<CustomerDTO>>(_customers);
        }

        [Benchmark]
        public void FastMapper() {
            var customerDTOs = global::FastMapper.TypeAdapter.Adapt<IList<Customer>, List<CustomerDTO>>(_customers);
        }
#endif
    }
}
