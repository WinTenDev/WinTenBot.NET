﻿using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Zizi.Bot.Services;

namespace Zizi.Bot.Handlers.Commands.Group
{
    public class PinCommand : CommandBase
    {
        private TelegramService _telegramService;

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args,
            CancellationToken cancellationToken)
        {
            _telegramService = new TelegramService(context);
            var msg = _telegramService.MessageOrEdited;
            var client = context.Bot.Client;

            var sendText = "Balas pesan yang akan di pin";

            var isAdmin = _telegramService.IsFromAdmin;
            if (!isAdmin)
            {
                Log.Warning("Pin message only for Admin on Current Chat");
                await _telegramService.DeleteAsync(msg.MessageId);
                return;
            }

            if (msg.ReplyToMessage != null)
            {
                await client.PinChatMessageAsync(
                        msg.Chat.Id,
                        msg.ReplyToMessage.MessageId,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await _telegramService.SendTextAsync(sendText, replyToMsgId: msg.MessageId)
                .ConfigureAwait(false);
        }
    }
}