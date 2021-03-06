﻿using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Zizi.Bot.Services;
using Zizi.Bot.Telegram;

namespace Zizi.Bot.Handlers
{
    internal class PingHandler : IUpdateHandler
    {
        private TelegramService _telegramService;

        public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
        {
            _telegramService = new TelegramService(context);
            var msg = context.Update.Message;

            var keyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("Ping", "PONG")
            );

            var sendText = "ℹ️ Pong!!";
            var isSudoer = msg.From.Id.IsSudoer();

            if (msg.Chat.Type == ChatType.Private && isSudoer)
            {
                sendText += "\n🎛 <b>Engine info.</b>";
                var getWebHookInfo = await _telegramService.GetWebhookInfo().ConfigureAwait(false);
                if (string.IsNullOrEmpty(getWebHookInfo.Url))
                {
                    sendText += "\n\n<i>Bot run in Poll mode.</i>";
                }
                else
                {
                    sendText += "\n\n<i>Bot run in Webhook mode.</i>" +
                                $"\nUrl WebHook: {getWebHookInfo.Url}" +
                                $"\nUrl Custom Cert: {getWebHookInfo.HasCustomCertificate}" +
                                $"\nAllowed Updates: {getWebHookInfo.AllowedUpdates}" +
                                $"\nPending Count: {(getWebHookInfo.PendingUpdateCount - 1)}" +
                                $"\nMax Connection: {getWebHookInfo.MaxConnections}" +
                                $"\nLast Error: {getWebHookInfo.LastErrorDate:yyyy-MM-dd}" +
                                $"\nError Message: {getWebHookInfo.LastErrorMessage}";
                }
            }

            await _telegramService.SendTextAsync(sendText, keyboard).ConfigureAwait(false);
        }
    }
}