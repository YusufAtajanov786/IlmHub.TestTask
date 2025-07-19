using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Configuration;
using System.Net;

var botToken = ""; // pls put you tg bot token 
var botClient = new TelegramBotClient(botToken);
var httpClient = new HttpClient();

var supportedStyles = new Dictionary<string, string>
{
    { "/help", "help" },
    { "/fun-emoji", "fun-emoji" },
    { "/avataaars", "avataaars" },
    { "/bottts", "bottts" },
    { "/pixel-art", "pixel-art" }
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync
);


Console.WriteLine("Bot ishga tushdi. Chiqish uchun Enter bosing...");
Console.ReadLine();

async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct)
{
    if (update.Type != UpdateType.Message || update.Message!.Type != MessageType.Text)
        return;

    var message = update.Message;
    var userId = message.From?.Id ?? 0;
    var text = message.Text?.Trim() ?? "";

    Console.WriteLine($"[{userId}] Kiruvchi: {text}");

    if (!text.StartsWith("/"))
    {
        await client.SendMessage(message.Chat.Id, "Iltimos, avatar olish uchun buyruqdan foydalaning.");
        return;
    }

    var parts = text.Split(' ', 2);
    var command = parts[0];
    var seed = parts.Length > 1 ? parts[1] : null;

    if (!supportedStyles.ContainsKey(command))
    {
        await client.SendMessage(message.Chat.Id,
            "Noma’lum buyruq. Quyidagilardan birini ishlating: /fun-emoji, /bottts, /avataaars, /pixel-art");
        return;
    }

    if (command.EndsWith("/help"))
    {
        await client.SendMessage(message.Chat.Id,
           $"Bu bot foydalanuvchiga oz avatarini yasashga yordam beradigan bot! \n" +
           $"Foydalanish uchun buyruklarni kiriting va ism kiritsanggiz, avatarni olishga muvofiq bolasiz!");

        return;
    }

    if (string.IsNullOrWhiteSpace(seed))
    {
        await client.SendMessage(message.Chat.Id,
            $"Iltimos, buyruqdan keyin matn (seed) kiriting. Misol: {command} Ali");
        return;
    }

    var style = supportedStyles[command];
    var url = $"https://api.dicebear.com/8.x/{style}/png?seed={WebUtility.UrlEncode(seed)}";

    try
    {
        var stream = await httpClient.GetStreamAsync(url);
        await client.SendPhoto(
            chatId: message.Chat.Id,
            photo: InputFile.FromStream(stream, $"{seed}.png"),
            caption: $"Avatar: {seed}"
        );

        Console.WriteLine($"[{userId}] Buyruq: {command}, Seed: {seed}, Holat: OK");
    }
    catch (HttpRequestException)
    {
        await client.SendMessage(message.Chat.Id, "Avatar yaratishda xatolik yuz berdi. Keyinroq urinib ko‘ring.");
        Console.WriteLine($"[{userId}] Buyruq: {command}, Seed: {seed}, Holat: Dicebear ERROR");
    }
    catch (Exception)
    {
        await client.SendMessage(message.Chat.Id, "Rasmni yuborishda xatolik yuz berdi.");
        Console.WriteLine($"[{userId}] Buyruq: {command}, Seed: {seed}, Holat: Telegram SEND ERROR");
    }
}

Task HandleErrorAsync(ITelegramBotClient client, Exception ex, CancellationToken ct)
{
    var errorMsg = ex switch
    {
        ApiRequestException apiEx => $"Telegram API xatosi: {apiEx.Message}",
        _ => ex.ToString()
    };

    Console.WriteLine($"Xato: {errorMsg}");
    return Task.CompletedTask;
}
