namespace SharpChain.Models;

using System.Text;
using System.Security.Cryptography;

public class Node
{
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890";
    private const string Difficulty = "0000";
    private const int DataLength = 256;
    private readonly SHA256 _coder = SHA256.Create();

    private int Id { get; set; }
    public LinkedList<Block> Chain { get; set; }
    
    public Node(int id)
    {
        Id = id;
        Chain = new LinkedList<Block>();
    }
    
    public Block CreateGenesis()
    {
        var genesis = new Block(0, "GENESIS", "hash", "data", 0, DateTime.Now);
        return genesis;
    }

    public Block CreateBlock()
    {
        var index = Chain.Count;
        var prevHash = Chain.Last?.Value.Hash;
        var data = GetData();
        var nonce = 0;
        var hash = GetHash(index, prevHash, data, nonce);

        var block = new Block(index, prevHash, hash, data, nonce);
        var minedBlock = Mine(block);

        return minedBlock;
    }

    private Block Mine(Block block)
    {
        var currHash = block.Hash;
        var currNonce = block.Nonce;
        while (!currHash.EndsWith(Difficulty))
        {
            currNonce += Id switch
            {
                1 => 1,
                2 => 5,
                _ => new Random().Next(1, 10)
            };

            currHash = GetHash(block.Index, block.PrevHash, block.Data, currNonce);
        }

        return new Block(block.Index, block.PrevHash, currHash, block.Data, currNonce, DateTime.Now);
    }

    private string GetHash(int index, string prevHash, string data, int nonce)
    {
        // using SHA256 hash = SHA256.Create();
        return string.Concat(_coder
            .ComputeHash(Encoding.UTF8.GetBytes(index + prevHash + data + nonce))
            .Select(item => item.ToString("x2"))); ;
    }

    private string GetData()
    {
        var random = new Random();
        var data = new string(Enumerable.Repeat(Letters, DataLength).Select(s => s[random.Next(s.Length)]).ToArray());
        return data;
    }

    public bool HandleReceivedBlock(Block block, int processId)
    {
        var blockInfo = GetBlockInfo(block);
        // If genesis
        if (Chain.Count == 0)
        {
            Chain.AddLast(block);
            Console.WriteLine($"Received genesis from Node {processId}: {blockInfo}");
            return true;
        }

        // If handle next block
        if (block.Index == Chain.Count)
        {
            var newBlockHash = GetHash(block.Index, block.PrevHash, block.Data, block.Nonce);
            
            if (block.Hash == newBlockHash)
            {
                Chain.AddLast(block);
                Console.WriteLine($"Received block from Node {processId}: {blockInfo}");
            }
            
            return true;
        }
        
        // If our chain is minor, request full new chain
        if (block.Index - Chain.Count > 0)
        {
            return false;
        }

        Thread.Sleep(1000);
        return true;
    }

    public void SetChain(LinkedList<Block> newChain, int processId)
    {
        if (ChainIsValid(newChain))
        {
            Chain = newChain;
        }

        Console.WriteLine($"Got full chain from Node {processId}");
    }
    
    private bool ChainIsValid(LinkedList<Block> chain)
    {
        foreach (var block in chain)
        {
            var blockHash = GetHash(block.Index, block.PrevHash, block.Data, block.Nonce);
            if (blockHash != block.Hash)
                return false;
        }

        return true;
    }
    
    public string GetBlockInfo(Block block)
    {
        string blockStr =
            $"Block(index={block.Index}, prevHash={block.PrevHash[..4]}, " +
            $"Hash={block.Hash[..4]}, Data={block.Data[..4]}, Nonce={block.Nonce}, " +
            $"Timestamp={block.TimeStamp.ToString()})";
        return blockStr;
    }
}