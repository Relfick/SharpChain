namespace SharpChain.Models;

public class Process
{
    private const int Port = 5010;
    
    private readonly string _host;
    private readonly Dictionary<int, string> _peerUrls;
    private readonly HttpClient _httpClient = new();

    private int Id { get; set; }
    private bool Received { get; set; }
    private Node Node { get; set; }
    
    public Process()
    {
        _host = GetHostName();
        Id = int.Parse(_host.Last().ToString());
        _peerUrls = GetPeerUrls(Id);
        Received = false;
        Node = new Node(Id);
    }

    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{_host}:{Port}/");
        var app = builder.Build();

        app.MapPost("/", NewBlockHandler);
        app.MapGet("/", SendChain);

        var server = new Thread(app.Run);
        var generator = new Thread(GeneratingNewBlock);

        server.Start();
        generator.Start();
        
        Thread.Sleep(1000);
    }

    private async void GeneratingNewBlock()
    {
        if (Id == 1)
        {
            var genesis = Node.CreateGenesis();
            Node.Chain.AddLast(genesis);
            var genesisInfo = Node.GetBlockInfo(genesis);
            Console.WriteLine($"Generated genesis: {genesisInfo}");
            await BroadcastBlock(genesis);
        }
        
        while (true)
        {
            if (Node.Chain.Count != 0)
            {
                if (!Received)
                {
                    var newBlock = Node.CreateBlock();
                    Node.Chain.AddLast(newBlock);
                    var broadcast = BroadcastBlock(newBlock);
                    
                    var blockInfo = Node.GetBlockInfo(newBlock);
                    Console.WriteLine($"Generated block: {blockInfo}");

                    await broadcast;
                }
            }
            
            Thread.Sleep(2000);
        }
    }

    private async Task BroadcastBlock(Block block)
    {
        if (!Received)
        {
            var tasks = new List<Task>();
            var blockDTO = new BlockDTO
            {
                ProcessId = this.Id,
                Index = block.Index,
                PrevHash = block.PrevHash,
                Hash = block.Hash,
                Data = block.Data,
                Nonce = block.Nonce,
                TimeStamp = block.TimeStamp
            };
            
            foreach (var peerUrl in _peerUrls.Values)
            {
                var task = _httpClient.PostAsJsonAsync(peerUrl, blockDTO);
                tasks.Add(task);
            }
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Connection refused");
            }
        }
    }

    private LinkedList<Block> SendChain()
    {
        return Node.Chain;
    }

    private async void NewBlockHandler(BlockDTO blockDTO)
    {
        Received = true;
        var block = new Block(
            blockDTO.Index, blockDTO.PrevHash, blockDTO.Hash,
            blockDTO.Data, blockDTO.Nonce, blockDTO.TimeStamp);
        var processId = blockDTO.ProcessId;
        
        bool success = Node.HandleReceivedBlock(block, processId);

        if (!success)
        {
            LinkedList<Block> newChain = await GetChain(processId);
            Node.SetChain(newChain, processId);
        }
        
        Thread.Sleep(1000);
        Received = false;
    }

    private async Task<LinkedList<Block>> GetChain(int processId)
    {
        var peerUrl = _peerUrls[processId];
        var chain = await _httpClient.GetFromJsonAsync<LinkedList<Block>>(peerUrl);
        if (chain != null)
        {
            return chain;
        }

        throw new NullReferenceException();
    }

    private Dictionary<int, string> GetPeerUrls(int currId)
    {
        string[] peerHosts = Environment.GetEnvironmentVariable("PEER_ADDRESSES")!.Split(',');
        
        var peerUrls = new Dictionary<int, string>();
        
        if (currId == 1)
        {
            peerUrls[2] = $"http://{peerHosts[0]}:{Port}/";
            peerUrls[3] = $"http://{peerHosts[1]}:{Port}/";
        }
        else if (currId == 2)
        {
            peerUrls[1] = $"http://{peerHosts[0]}:{Port}/";
            peerUrls[3] = $"http://{peerHosts[1]}:{Port}/";
        }
        else
        {
            peerUrls[1] = $"http://{peerHosts[0]}:{Port}/";
            peerUrls[2] = $"http://{peerHosts[1]}:{Port}/";
        }

        return peerUrls;
    }

    private string GetHostName()
    {
        string currHostName = Environment.GetEnvironmentVariable("NODE_ADDRESS")!;
        return currHostName;
    }
}