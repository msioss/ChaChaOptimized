using System;
using ChaChaOptimization;

Console.WriteLine("ChaCha Benchmark (Optimized)");
Console.WriteLine($".NET:       {Environment.Version}");
Console.WriteLine($"OS:         {Environment.OSVersion}");
Console.WriteLine($"CPU Cores:  {Environment.ProcessorCount}\n");

var bench = new BenchmarkRunner();

bench.Warmup();
bench.RunAll();

Console.WriteLine("Done.");