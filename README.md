# BatMap - The Mapper we deserve, not the one we need.
<img src="https://image.ibb.co/jDUyWQ/logo_64x64.png" alt="🦇 BatMap" align="middle"> **Opininated (yet another) mapper, mainly to convert between EF Entities and DTOs.** 

Supports .Net Standard 1.0.

[![Build status](https://ci.appveyor.com/api/projects/status/m6gh59s077a52xen?svg=true)](https://ci.appveyor.com/project/DogusTeknoloji/batmap)
[![Coverage Status](https://coveralls.io/repos/github/DogusTeknoloji/BatMap/badge.svg?branch=master)](https://coveralls.io/github/DogusTeknoloji/BatMap?branch=master)
[![NuGet Badge](https://buildstats.info/nuget/BatMap)](https://www.nuget.org/packages/BatMap/)
[![Join the chat at https://gitter.im/NaNaNaNaBatMap/Lobby](https://badges.gitter.im/NaNaNaNaBatMap/Lobby.svg)](https://gitter.im/NaNaNaNaBatMap/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![GitHub issues](https://img.shields.io/github/issues/DogusTeknoloji/BatMap.svg)](https://github.com/DogusTeknoloji/BatMap/issues)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/DogusTeknoloji/BatMap/master/LICENSE)

[![GitHub stars](https://img.shields.io/github/stars/DogusTeknoloji/BatMap.svg?style=social&label=Star)](https://github.com/DogusTeknoloji/BatMap)
[![GitHub forks](https://img.shields.io/github/forks/DogusTeknoloji/BatMap.svg?style=social&label=Fork)](https://github.com/DogusTeknoloji/BatMap)

Let's first obey the number one rule for mappers, a benchmark (using [BenchmarkDotNet](http://benchmarkdotnet.org/)):

|        Method |     Mean |
|-------------- |---------:|
|   HandWritten | 1.143 ms |
|        BatMap :boom: | 2.000 ms |
|    SafeMapper | 2.933 ms |
|       Mapster | 2.942 ms |
|    AutoMapper | 3.497 ms |
|    TinyMapper | 4.172 ms |
| ExpressMapper | 6.955 ms |
|    FastMapper | 9.203 ms |

<sup>Results may (probably) vary. Latest run can bee seen on [Appveyor Build](https://ci.appveyor.com/project/DogusTeknoloji/batmap).</sup>
    
* Fast (enough)
* NOT over-engineered, code is really simple
* Instantiatable mapper
* Convention based, zero configuration static shortcut exists too (obviously named Mapper)
* Does not crash when faced with circular-dependencies during registration
* In fact, can resolve recurring instances to same target instance (yaay no StackOverflowException!)
* Can project IQueryable\<TSource\> to IQueryable\<TTarget\> with respect to includes (via auto-detection or with custom parameters)
* and much more...

## API
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

## Where can I get it?

You can install [BatMap](https://www.nuget.org/packages/BatMap/) from the package manager console:

```
PM> Install-Package BatMap
```

## Documentation
You might want to visit [wiki](https://github.com/DogusTeknoloji/BatMap/wiki) for more.

***

Developed with :heart: at [Doğuş Teknoloji](http://www.d-teknoloji.com.tr).
