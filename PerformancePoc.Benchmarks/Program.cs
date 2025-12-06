using BenchmarkDotNet.Running;

namespace PerformancePoc.Benchmarks
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "test")
            {
                await FunctionalTest.RunAsync();
                return;
            }

            var summary = BenchmarkRunner.Run<PerformanceBenchmarks>();
        }
    }
}
