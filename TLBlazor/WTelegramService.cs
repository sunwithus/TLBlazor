//WTelegramService.cs
using TL;

public class WTelegramService : BackgroundService
{
    public readonly WTelegram.Client Client;
    public User User => Client.User;
    public string ConfigNeeded { get; set; } = "connecting";
    public Messages_Chats Chats { get; private set; }

    private readonly IConfiguration _config;
    private readonly ILogger<WTelegramService> _logger;
    
    public WTelegramService(IConfiguration config, ILogger<WTelegramService> logger)
    {
        _config = config;
        _logger = logger;
        WTelegram.Helpers.Log = (lvl, msg) => logger.Log((LogLevel)lvl, msg);
        Client = new WTelegram.Client(what => _config[what]);
    }
    public override void Dispose()
    {
        Client.Dispose();
        base.Dispose();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConfigNeeded = await DoLogin(_config["phone_number"]);
        Chats = await Client.Messages_GetAllChats();
    }
    public async Task<string> DoLogin(string loginInfo)
    {
        return ConfigNeeded = await Client.Login(loginInfo);
    }
}
