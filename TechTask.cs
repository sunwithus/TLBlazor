ТЗ

на C#
использую библиотеку WTelegramClient
нужно реализовать выгрузку собщений из групп/каналов и комментариев к этим сообщениям

давай создадим страницу razor для отображения комментариев
а на странице ChatMessages.razor сделай ссылку перед <hr /> для перехода на страницу комментариев


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
            @if (WT.Chats == null)
            {
                <MudText Typo="Typo.body1" Class="mud-theme-primary" Style="display:flex; justify-content:space-between;">
                    Загрузка каналов/групп...
                </MudText>
            }
            else
            {
                @foreach (var (id, chat) in WT.Chats.chats)
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
        await Task.Delay(5000);
        if (WT.Chats == null)
        {
            //WT.Chats = await WT.Client.Messages_GetAllChats(); // chats = groups/channels (does not include users dialogs)
        }

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


WTelegramService.cs - служба для работы с библиотекой WTelegramClient//WTelegramService.cs
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

ChatMessages.razor - страница для выгрузки собщений от групп/каналов и комментариев к ним
@*ChatMessages.razor*@

@using MudBlazor
@using TL

@inject WTelegramService WT
@inject NavigationManager Navigation

@page "/chat/{id:long}"

<h5>Данные с id => @id (@ChatName):</h5>

@if (WT.User == null)
{
    <p>Вы не автроизованы.</p>
}
else
{
    @if (messages == null)
    {
        <p>Загрузка...</p>
    }
    else
    {
        <MudText Color="Color.Warning" Typo="Typo.subtitle2">messages.Count = @messages.Count</MudText>
        @foreach (var msg in messages.Messages)
        {
            <p style="white-space:pre-wrap">@msg.Date.ToString("yyyy-MM-dd HH:mm"): @GetMessageText(msg)
                @if (msg is Message message)
                {
                    @if (message.media != null)
                    {
                        @switch (message.media)
                        {
                            case MessageMediaPhoto photo:
                                <span> - Картинка (@photo.ToString())</span>
                                break;
                            case MessageMediaContact contact:
                                <span> - Котнакт (@contact.ToString())</span>
                                break;
                            case MessageMediaDice sticker:
                                <span> - Анимированный стикер (@sticker.ToString())</span>
                                break;
                            case MessageMediaGeo geo:
                                <span> - Гео данные (@geo.ToString())</span>
                                break;
                            case MessageMediaWebPage webPage:
                                <span> - Веб страница @webPage.ToString()</span>
                                break;
                            case MessageMediaStory story:
                                <span> - Сторис @story.ToString()</span>
                                break;
                            case MessageMediaDocument doc:
                                <span> - Видео или Документ (@doc.GetType())</span>
                                break;
                            default:
                                <span> - Неизвестный тип медиафайла (@message.media.GetType())</span>
                                break;
                        }
                    }
                }

            </p>
            <hr />
        }
    }
}

@code {

    [Parameter]
    public long id { get; set; } = 0;
    private string ChatName = "";

    private Messages_MessagesBase messages;

    protected override async Task OnParametersSetAsync()
    {
        if (WT.Chats != null && WT.Chats.chats.TryGetValue(id, out var chat))
        {
            InputPeer inputPeer;
            if (chat is Channel channel)
            {
                inputPeer = new InputPeerChannel(channel.ID, channel.access_hash);
            }
            else
            {
                inputPeer = new InputPeerChat(chat.ID);
            }
            ChatName = chat.Title;

            int offset = 0;

            //https://corefork.telegram.org/methods messages.getHistory == Messages_GetHistory
            //messages.getHistory#4423e6c5 peer:InputPeer offset_id:int offset_date:int add_offset:int limit:int max_id:int min_id:int hash:long = messages.Messages;
            //peer, StartWithId, TillDate, SkipElements, LimitResult,
            messages = await WT.Client.Messages_GetHistory(peer: inputPeer, offset_id: offset, add_offset: 0, limit: 50);
            StateHasChanged();
        }
        else
        {
            messages = null;
        }
    }

    private string GetMessageText(MessageBase msg)
    {
        var text = msg switch
        {
            Message message => message.message,
            MessageService service => $"[Сервисное сообщение: {service.action}]",
            _ => "[Unknown message type]"
        };

        return text;
    }

}
