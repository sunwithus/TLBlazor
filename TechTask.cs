ТЗ

на C#
использую библиотеку WTelegramClient
нужно реализовать выгрузку собщений из групп/каналов и комментариев к этим сообщениям


NavMenu.razor - загружает список групп/каналов
@*NavMenu.razor*@

@using MudBlazor
@using TL

@inject WTelegramService WT


<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">TL.Net</a>
    </div>
</div>

<input type="checkbox" title="Navigation menu" class="navbar-toggler" />

<div class="nav-scrollable" onclick="document.querySelector('.navbar-toggler').click()">
    <nav class="flex-column">

        @if (WT.User == null)
        {
            <p>Please complete the login first.</p>
            <div class="nav-item px-3">
                <NavLink class="nav-link" href="@($"/")">
                    <span class="bi bi-plus-square-fill-nav-menu" aria-hidden="true"></span>Home
                </NavLink>
            </div>
        }
        else
        {
            @if (chats == null)
            {
                <p>Loading chats...</p>
            }
            else
            {
                @foreach (var (id, chat) in chats.chats)
                {
                    switch (chat)
                    {
                        case Chat smallgroup when smallgroup.IsActive:
                            <div class="nav-item px-3">
                                <NavLink class="nav-link" href="@($"/chat/{id}")">
                                    <MudText Style="display:flex; justify-content:space-between;">
                                        <b>@smallgroup.title</b>&emsp;<i>@($"(minCh:{smallgroup.participants_count})")</i>
                                    </MudText>
                                </NavLink>
                            </div>
                            break;
                        case Channel channel when channel.IsChannel:
                            <div class="nav-item px-3">
                                <NavLink class="nav-link" href="@($"/chat/{id}")">
                                    <MudText Style="display:flex; justify-content:space-between;">
                                        <b>@($"{channel.username}: {channel.title} ")</b>&emsp;<i>@($"(Ch:{channel.participants_count})")</i>
                                    </MudText>
                                </NavLink>
                            </div>
                            break;
                        case Channel group: // no broadcast flag => it's a big group, also called supergroup or megagroup
                            <div class="nav-item px-3">
                                <NavLink class="nav-link" href="@($"/chat/{id}")">
                                    <MudText Style="display:flex; justify-content:space-between;">
                                        <b>@($"{group.username}: {group.title}")</b>&emsp;<i>@($"(Group)")</i>
                                    </MudText>
                                </NavLink>
                            </div>
                            break;
                    }
                }
            }
        }

    </nav>
</div>


@code
{
    public Messages_Chats chats = null;
    protected override async Task OnInitializedAsync()
    {
        await Task.Delay(100);
        chats = await WT.Client.Messages_GetAllChats(); // chats = groups/channels (does not include users dialogs)

    }
}

//Program.cs
using TLBlazor.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<WTelegramService>();
builder.Services.AddHostedService(provider => provider.GetService<WTelegramService>());

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

WTelegramService.cs - служба для работы с библиотекой WTelegramClient
//WTelegramService.cs 
using TL;

public class WTelegramService : BackgroundService
{
    public readonly WTelegram.Client Client;
    public User User => Client.User;
    public string ConfigNeeded { get; set; } = "connecting";

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
    }
    public async Task<string> DoLogin(string loginInfo)
    {
        return ConfigNeeded = await Client.Login(loginInfo);
    }
}

ChatMessages.razor - страница для выгрузки собщений от групп/каналов и комментариев к ним
@*ChatMessages.razor*@

@using TL

@inject WTelegramService WT
@inject NavigationManager Navigation

@page "/chat/{id:long}"

<h2>Messages from chat @id:</h2>

@if (messages != null)
{
    <h5>messages.Count = @messages.Count</h5>
}
else
{
    <h5>messages is null</h5>
}

@if (WT.User == null)
{
    <p>Please complete the login first.</p>
}
else
{
    @if (messages == null)
    {
        <p>Loading messages...</p>

    }
    else
    {
        @foreach (var msg in messages.Messages)
        {
            <p>@msg.Date.ToString("yyyy-MM-dd HH:mm"): @GetMessageText(msg)</p>
        }
    }
}

@code {

    [Parameter]
    public long id { get; set; } = 0;

    private Messages_MessagesBase messages;

    protected override async Task OnInitializedAsync()
    {
        var dialogs = await WT.Client.Messages_GetAllChats();

        if (!dialogs.chats.TryGetValue(id, out var chat))
        {
            messages = null;
            return;
        }

        InputPeer inputPeer;
        if (chat is Channel channel)
        {
            inputPeer = new InputPeerChannel(channel.ID, channel.access_hash);
        }
        else
        {
            inputPeer = new InputPeerChat(chat.ID);
        }

        messages = await WT.Client.Messages_GetHistory(inputPeer, 20);
    }


    private string GetMessageText(MessageBase msg)
    {
        return msg switch
        {
            Message message => message.message,
            MessageService service => $"[Service message: {service.action}]",
            _ => "[Unknown message type]"
        };
    }
}

правильный ли подход для моего технического задания или лучше по другому?
нужно убрать повторную загрузку var dialogs = await WT.Client.Messages_GetAllChats(), ведь в NavMenu.razor уже есть chats = await WT.Client.Messages_GetAllChats()
сейчас при клике на ссылку с id другой группы обновления не происходит, надо чтоб загружались сообщения с другого id



