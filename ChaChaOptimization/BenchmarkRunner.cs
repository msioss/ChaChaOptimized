using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using ChaChaOptimization.Structs;

namespace ChaChaOptimization;

public sealed class BenchmarkRunner
{
    private readonly int[] blockSizes = { 1024, 10240, 102400, 1048576 };
    private readonly int[] roundsOptions = { 8, 12, 20 };
    private readonly int iterations = 10;
    
    private readonly byte[] key;
    private readonly byte[] nonce;

    public BenchmarkRunner()
    {
        key = new byte[32];
        nonce = new byte[12];
        
        // Генерація випадкових ключа та nonce
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
            rng.GetBytes(nonce);
        }
    }

    /// <summary>
    /// JIT warmup для точніших вимірювань.
    /// </summary>
    public void Warmup()
    {
        Console.WriteLine("Warming up JIT...");
        
        int[] warmSizes = { 1024, 10240 };
        int[] warmRounds = { 8, 12, 20 };
        
        var warmData = new byte[10240];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(warmData);

        foreach (var r in warmRounds)
        {
            foreach (var size in warmSizes)
            {
                var data = new byte[size];
                Array.Copy(warmData, data, size);
                
                var c = new ChaCha(key, nonce, 1, r);
                
                for (int i = 0; i < 15; i++)
                    c.Process(data);
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        Console.WriteLine("Warmup done.\n");
    }

    /// <summary>
    /// Запускає всі бенчмарки та записує результати в CSV.
    /// </summary>
    public void RunAll()
    {
        using var csv = new StreamWriter("benchmark-results.csv");
        csv.WriteLine("Algorithm,BlockSize_KB,Latency_us,CPU_ms,Throughput_MBps");

        foreach (var rounds in roundsOptions)
        {
            string algoName = $"ChaCha{rounds}";
            Console.WriteLine($"\n=== {algoName} ===");

            foreach (var blockSize in blockSizes)
            {
                double totalLatency = 0;
                double totalCpu = 0;
                double totalThroughput = 0;

                // Генерація тестових даних один раз
                var sourceData = new byte[blockSize];
                using (var rng = RandomNumberGenerator.Create())
                    rng.GetBytes(sourceData);

                for (int iter = 0; iter < iterations; iter++)
                {
                    // ✅ ВИПРАВЛЕНО: копіюємо дані для кожної ітерації
                    var data = new byte[blockSize];
                    Array.Copy(sourceData, data, blockSize);

                    var result = RunSingle(blockSize, rounds, data);
                    
                    totalLatency += result.Latency;
                    totalCpu += result.Cpu;
                    totalThroughput += result.Throughput;
                }

                double avgLatency = totalLatency / iterations;
                double avgCpu = totalCpu / iterations;
                double avgThroughput = totalThroughput / iterations;

                csv.WriteLine($"{algoName},{blockSize / 1024.0:F2},{avgLatency:F2},{avgCpu:F2},{avgThroughput:F2}");

                Console.WriteLine($"  {blockSize / 1024.0,7:F2} KB: " +
                                 $"Latency={avgLatency,7:F2}µs | " +
                                 $"CPU={avgCpu,6:F2}ms | " +
                                 $"Throughput={avgThroughput,7:F2}MB/s");
            }
        }

        Console.WriteLine("\n✓ Results saved to benchmark-results.csv");
    }

    /// <summary>
    /// Запускає один бенчмарк з вимірюванням часу та CPU.
    /// </summary>
    private BenchmarkResult RunSingle(int blockSize, int rounds, byte[] data)
    {
        // ✅ ВИПРАВЛЕНО: використовуємо ті самі ключі
        var cipher = new ChaCha(key, nonce, 1, rounds);
        var proc = Process.GetCurrentProcess();
        
        // Вимірювання CPU time ДО операції
        var cpuBefore = proc.TotalProcessorTime;
        
        // Вимірювання wall-clock time
        var sw = Stopwatch.StartNew();
        cipher.Process(data);
        sw.Stop();
        
        // Вимірювання CPU time ПІСЛЯ операції
        var cpuAfter = proc.TotalProcessorTime;

        double latencyUs = sw.Elapsed.TotalMilliseconds * 1000;      // Конвертуємо в мікросекунди
        double cpuMs = (cpuAfter - cpuBefore).TotalMilliseconds;      // CPU time в мс
        double throughputMBps = (blockSize / 1048576.0) / sw.Elapsed.TotalSeconds; // МБ/сек

        return new BenchmarkResult
        {
            Latency = latencyUs,
            Cpu = cpuMs,
            Throughput = throughputMBps
        };
    }
}