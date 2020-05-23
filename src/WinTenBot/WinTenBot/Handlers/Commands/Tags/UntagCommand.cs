﻿using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using WinTenBot.Helpers;
using WinTenBot.Providers;
using WinTenBot.Services;

namespace WinTenBot.Handlers.Commands.Tags
{
    public class UntagCommand : CommandBase
    {
        private TagsService _tagsService;
        private TelegramService _telegramService;

        public UntagCommand()
        {
            _tagsService = new TagsService();
        }

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args,
            CancellationToken cancellationToken)
        {
            _telegramService = new TelegramService(context);
            var msg = context.Update.Message;

            var isAdmin = await _telegramService.IsAdminGroup();
            var tagVal = args[0];
            var sendText = "Perintah Untag hanya untuk ngadmin.";

            if (isAdmin)
            {
                await _telegramService.SendTextAsync("Memeriksa..");
                var isExist = await _tagsService.IsExist(_telegramService.Message.Chat.Id, tagVal);
                if (isExist)
                {
                    Log.Information($"Sedang menghapus tag {tagVal}");
                    var unTag = await _tagsService.DeleteTag(_telegramService.Message.Chat.Id, tagVal);
                    if (unTag)
                    {
                        sendText = $"Hapus tag {tagVal} berhasil";
                    }

                    await _telegramService.EditAsync(sendText);
                    return;
                }
                else
                {
                    sendText = $"Tag {tagVal} tidak di temukan";
                }
            }

            await _telegramService.SendTextAsync(sendText);
        }
    }
}