using SharpChain.Models;

namespace SharpChain;

public static class Program
{
    public static Task Main(string[] args)
    {
        int port = int.Parse(args[0]);
        
        Process process = new Process(port);
        process.Start();
        return Task.CompletedTask;
    }
}