namespace SharpChain.Models;

public record BlockDTO
{
    public int ProcessId { get; set; }
    public int Index { get; set; }
    public string PrevHash { get; set; }
    public string Hash { get; set; }
    public string Data { get; set; }
    public int Nonce { get; set; }
    public DateTime? TimeStamp { get; set; }
}