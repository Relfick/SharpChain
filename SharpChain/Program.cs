using SharpChain.Models;

namespace SharpChain;

public static class Program
{
    public static Task Main()
    {
        Process process = new Process();
        
        process.Start();
        return Task.CompletedTask;
    }
}