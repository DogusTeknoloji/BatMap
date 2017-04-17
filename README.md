# BatMap - The Mapper we deserve, not the one we need.
Opininated (yet another) mapper, mainly to convert between EF Entities and DTOs.

[![Build Status](https://travis-ci.org/DogusTeknoloji/BatMap.svg?branch=master)](https://travis-ci.org/DogusTeknoloji/BatMap)
[![Coverage Status](https://coveralls.io/repos/github/DogusTeknoloji/BatMap/badge.svg?branch=master)](https://coveralls.io/github/DogusTeknoloji/BatMap?branch=master)
[![Join the chat at https://gitter.im/NaNaNaNaBatMap/Lobby](https://badges.gitter.im/NaNaNaNaBatMap/Lobby.svg)](https://gitter.im/NaNaNaNaBatMap/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![GitHub stars](https://img.shields.io/github/stars/badges/shields.svg?style=social&label=Star)](https://github.com/DogusTeknoloji/BatMap)
[![GitHub forks](https://img.shields.io/github/forks/badges/shields.svg?style=social&label=Fork)](https://github.com/DogusTeknoloji/BatMap)

Let's first obey the number one rule for mappers, a benchmark (using [BenchmarkDotNet](http://benchmarkdotnet.org/)):

|        Method |      Mean |
|-------------- |---------- |
|        BatMap :boom:| 1.8563 ms :boom:|
|       Mapster | 2.0414 ms |
|    SafeMapper | 2.0589 ms |
|   HandWritten | 2.1000 ms |
|    AutoMapper | 2.7422 ms |
|    TinyMapper | 2.8609 ms |
| ExpressMapper | 4.9961 ms |
|    FastMapper | 5.7874 ms |

<sup>Results may (probably) vary</sup>
    
* Fast (enough)
* NOT over-engineered, code is really simple
* Instantiatable mapper
* Convention based, zero configuration static shortcut exists too (obviously named Mapper)
* Does not crash when faced with circular-dependencies during registration
* In fact, can resolve recurring instances to same target instance (yaay no StackOverflowException!)
* Can project IQueryable\<TSource\> to IQueryable\<TTarget\> with respect to includes (via auto-detection or with custom parameters)
* and much more...

# API
Registration with static API:
```csharp
Mapper.RegisterMap<Customer, CustomerDTO>();
```
or use an instance:
```csharp
var mapper = new MapConfiguration(dynamicMapping: DynamicMapping.MapAndCache, preserveReferences: true);
mapper.RegisterMap<Customer, CustomerDTO>();
```
Note: You don't have to register type mappings when using a MapConfiguration with Dynamic Mapping enabled (like the static API uses).


You can customize expressions for members:
```csharp
mapper.RegisterMap<Order, OrderDTO>(b => b.MapMember(o => o.Price, (o, mc) => o.Count * o.UnitPrice));
```

Map an object:
```csharp
Mapper.Map<CustomerDTO>(customer);
```
Map an enumerable:
```csharp
customers.MapTo<Customer, CustomerDTO>(preserveReferences: true);  // extension methods FTW!
```
Project a query:
```csharp
customerQuery.ProjectTo<CustomerDTO>(checkIncludes: true);
```
or with expanding specific navigations:
```csharp
customerQuery.ProjectTo<Customer, CustomerDTO>(c => c.Addresses, c => c.Orders);
```

Note: If you want to change mapping behavior, create a class that inherits from ExpressionProvider, override CreateMemberBinding and inject an instance of your class to MapConfiguration.

Developed with :heart: at [Doğuş Teknoloji](http://www.d-teknoloji.com.tr).
