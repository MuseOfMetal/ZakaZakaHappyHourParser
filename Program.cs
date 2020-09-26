using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Discord.Webhook;
namespace HappyHourParser
{
    class Program
    {
        static TelegramBotClient tBot;
        static HappyHour HappyHour;

        static void Main(string[] args)
        {
            tBot = new TelegramBotClient(Config.GetConfig().TelegramBotToken);
            HappyHour = new HappyHour();
            HappyHour.Notify += HappyHour_Notify;
            HappyHour.StartMonitoring();
            tBot.OnMessage += TBot_OnMessage;
            tBot.StartReceiving();
            while (true)
            {
                Thread.Sleep(int.MaxValue);
            }
        }

        private async static void TBot_OnMessage(object sender, MessageEventArgs e)
        {
            string message = e.Message.Text;
            int userid = e.Message.From.Id;
            if (message.StartsWith("/"))
            {
                if (message == "/start")
                    await tBot.SendTextMessageAsync(
                        userid,
                        "Доброго времени суток!\n\n" +
                        "Для того чтобы подписаться на обновления страницы \"Happy Hour\", введите команду /subscribe.\n\n" +
                        "Если вы хотите добавить Discord Webhook, введите команду /addwebhook {url}");
                else if (message == "/subscribe")
                {
                    if (Config.GetConfig().TelegramUserIDs.Contains(userid))
                    {
                        await tBot.SendTextMessageAsync(
                            userid,
                            "Вы уже подписаны!");
                        return;
                    }
                    Config.GetConfig().TelegramUserIDs.Add(userid);
                    Config.Save();
                    await tBot.SendTextMessageAsync(
                        userid,
                        "Вы успешно подписались на обновления страницы \"Happy Hour\"\n\n" +
                        "Если вы желаете отписаться от обновлений, введите команду /unsubscribe");
                }
                else if (message == "/unsubscribe")
                {
                    if (Config.GetConfig().TelegramUserIDs.Contains(userid))
                    {
                        Config.GetConfig().TelegramUserIDs.Remove(userid);
                        Config.Save();
                        await tBot.SendTextMessageAsync(
                            userid,
                            "Вы успешно отписались от обновлений!");
                    }
                }
                else if (message.Contains("/addwebhook"))
                {
                    string[] args = message.Split(' ');
                    if (args.Length != 2)
                        await tBot.SendTextMessageAsync(
                            userid,
                            "Неправильно введены аргументы\n\n" +
                            "/addwebhook {url}");
                    else
                    {
                        if (Config.GetConfig().DiscordWebhookURLs.Contains(args[1]))
                            await tBot.SendTextMessageAsync(
                                userid,
                                "Данный Webhook уже добавлен!");
                        try
                        {
                            await new DiscordWebhookClient(args[1]).SendMessageAsync("Проверка...");
                            Config.GetConfig().DiscordWebhookURLs.Add(args[1]);
                            Config.Save();
                            await tBot.SendTextMessageAsync(
                                userid,
                                "Успешно добавлено!\n\n" +
                                "Если вы хотите удалить Discord Webhook, удалите с сервера этот Webhook");
                        }
                        catch
                        {
                            await tBot.SendTextMessageAsync(
                                userid,
                                "Недействительная ссылка");
                        }
                    }
                }
            }
        }

        private async static void HappyHour_Notify(object sender, List<Product> newProducts)
        {
            string text = "====Новые игры====\n\n";

            foreach (var item in newProducts)
            {
                text +=
                    $"Название: {item.Name}\n" +
                    $"Скидка: {item.Discount} ({item.DiscountSum} RUB)\n" +
                    $"Цена: {item.Price} RUB\n\n";
            }
            text += "==================";
            List<string> tokensToDelete = new List<string>();
            foreach (var item in Config.GetConfig().DiscordWebhookURLs)
            {
                try
                {
                    await new DiscordWebhookClient(item).SendMessageAsync(text);
                }
                catch
                {
                    tokensToDelete.Add(item);
                }
            }
            foreach (var item in tokensToDelete)
            {
                Config.GetConfig().DiscordWebhookURLs.Remove(item);
            }
            List<int> idsToDelete = new List<int>();
            foreach (var item in Config.GetConfig().TelegramUserIDs)
            {
                try
                {
                    await tBot.SendTextMessageAsync(item, text);
                }
                catch
                {
                    idsToDelete.Add(item);
                }
            }
            foreach (var item in idsToDelete)
            {
                Config.GetConfig().TelegramUserIDs.Remove(item);
            }
        }
    }
}
