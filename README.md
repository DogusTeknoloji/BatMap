# BatMap - The Mapper we deserve, not the one we need.
Opininated (yet another) mapper, mainly to convert between EF Entities and DTOs.


Let's first obey the number one rule for mappers, a benchmark;

|      Method |      Mean |    StdDev |
|------------ |---------- |---------- |
|     Bat_Map:boom: | 1.9211 ms:boom: | 0.0108 ms:boom: |
| Mapster_Map | 2.0357 ms | 0.0161 ms |
|    Safe_Map | 2.0797 ms | 0.0273 ms |
|      ByHand | 2.1018 ms | 0.0906 ms |
|    Auto_Map | 2.8359 ms | 0.0253 ms |
|    Tiny_Map | 2.9644 ms | 0.0768 ms |
| Express_Map | 5.1630 ms | 0.0130 ms |
|    Fast_Map | 5.8862 ms | 0.0277 ms |
    
    
* Fast (enough)
* NOT over-engineered, code is really simple
* Instantiatable mapper
* Convention based, zero configuration static shortcut exists too (of course named Mapper)
* Does not crash when faced with circular-dependencies during registration
* In fact, can resolve recurring instances to same target instance (yaay no StackOverflowException!)
* Can project IQueryable\<TSource\> to IQueryable\<TTarget\> with respect to includes (via auto-detection or with custom parameters)
* You can project IEnumerable\<TSource\>'s too

Developed with :heart: at Doğuş Teknoloji.
