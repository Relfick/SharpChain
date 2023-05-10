namespace SharpChain.Models;

using SharpChain.Models;

public class Process
{
    private const string Host = "localhost";
    private readonly Dictionary<int, string> _peerUrls;
    private readonly HttpClient _httpClient = new();

    private int Id { get; set; }
    private int Port { get; set; }
    private bool Received { get; set; }
    private Node Node { get; set; }
    
    public Process(int port)
    {
        Id = port % 10;
        Port = port;
        _peerUrls = GetPeerUrls(port);
        Received = false;
        Node = new Node(Id);
    }

    public void Start()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{Host}:{Port}/");
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
            
            Thread.Sleep(3000);
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
        
        Thread.Sleep(2000);
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

    private static Dictionary<int, string> GetPeerUrls(int currPort)
    {
        var port1 = int.Parse(Environment.GetEnvironmentVariable("FIRST_PROCESS_PORT")!);
        var port2 = int.Parse(Environment.GetEnvironmentVariable("SECOND_PROCESS_PORT")!);
        var port3 = int.Parse(Environment.GetEnvironmentVariable("THIRD_PROCESS_PORT")!);
        
        var peerUrls = new Dictionary<int, string>();
        
        if (currPort == port1)
        {
            peerUrls[2] = $"http://{Host}:{port2}/";
            peerUrls[3] = $"http://{Host}:{port3}/";
        }
        else if (currPort == port2)
        {
            peerUrls[1] = $"http://{Host}:{port1}/";
            peerUrls[3] = $"http://{Host}:{port3}/"; 
        }
        else
        {
            peerUrls[1] = $"http://{Host}:{port1}/";
            peerUrls[2] = $"http://{Host}:{port2}/"; 
        }

        return peerUrls;
    }
}