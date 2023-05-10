namespace SharpChain.Models;

public class Block
{
    public Block(int index, string prevHash, string hash, string data, int nonce, DateTime? timeStamp = null)
    {
        Index = index;
        PrevHash = prevHash;
        Hash = hash;
        Data = data;
        Nonce = nonce;
        TimeStamp = timeStamp;
    }

    public int Index { get; set; }
    public string PrevHash { get; set; }
    public string Hash { get; set; }
    public string Data { get; set; }
    public int Nonce { get; set; }
    public DateTime? TimeStamp { get; set; }
}