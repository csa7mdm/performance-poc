```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.1 (25B78) [Darwin 25.1.0]
Apple M3 Pro, 1 CPU, 11 logical and 11 physical cores
.NET SDK 9.0.109
  [Host]     : .NET 9.0.8 (9.0.8, 9.0.825.36511), Arm64 RyuJIT armv8.0-a
  Job-TABGLQ : .NET 9.0.8 (9.0.8, 9.0.825.36511), Arm64 RyuJIT armv8.0-a

IterationCount=5  LaunchCount=1  RunStrategy=Monitoring  
WarmupCount=1  

```
| Method         | N   | PayloadSize | Mean        | Error       | StdDev      | Median      | Allocated  |
|--------------- |---- |------------ |------------:|------------:|------------:|------------:|-----------:|
| Db_Produce_Seq | 100 | 1024        | 30,408.5 μs |  7,771.6 μs | 2,018.25 μs | 31,378.4 μs |  752.21 KB |
| Mq_Produce_Seq | 100 | 1024        |    282.7 μs |    345.6 μs |    89.75 μs |    250.3 μs |   562.5 KB |
| Db_Consume_Seq | 100 | 1024        | 95,004.1 μs | 23,055.9 μs | 5,987.55 μs | 92,337.0 μs | 1543.42 KB |
| Mq_Consume_Seq | 100 | 1024        |  9,604.4 μs | 14,852.3 μs | 3,857.09 μs |  8,013.8 μs | 1595.68 KB |
| Db_Produce_Par | 100 | 1024        | 12,702.4 μs | 19,707.0 μs | 5,117.84 μs | 10,815.3 μs |  778.59 KB |
| Mq_Produce_Par | 100 | 1024        |    414.6 μs |  1,905.8 μs |   494.94 μs |    171.5 μs |  565.09 KB |
| Db_Consume_Par | 100 | 1024        | 51,421.8 μs | 19,872.7 μs | 5,160.89 μs | 53,531.8 μs | 1599.56 KB |
| Mq_Consume_Par | 100 | 1024        | 10,175.7 μs |  1,446.1 μs |   375.55 μs | 10,043.8 μs |  852.66 KB |
