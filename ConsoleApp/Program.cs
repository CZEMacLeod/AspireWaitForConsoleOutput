namespace ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            await Task.Delay(TimeSpan.FromSeconds(10));
            Console.WriteLine("Ready Now...");

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                Console.WriteLine("Tick...");
            }
        }
    }
}
