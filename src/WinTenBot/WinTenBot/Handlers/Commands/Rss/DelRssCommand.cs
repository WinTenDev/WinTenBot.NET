﻿using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using WinTenBot.Helpers;
using WinTenBot.Providers;
using WinTenBot.Services;

namespace WinTenBot.Handlers.Commands.Rss
{
    public class DelRssCommand : CommandBase
    {
        private RssService _rssService;
        private TelegramProvider _telegramProvider;

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args,
            CancellationToken cancellationToken)
        {
            _telegramProvider = new TelegramProvider(context);
            _rssService = new RssService(context.Update.Message);

            var isAdminOrPrivateChat = await _telegramProvider.IsAdminOrPrivateChat();
            if (isAdminOrPrivateChat)
            {
                var urlFeed = _telegramProvider.Message.Text.GetTextWithoutCmd();

                await _telegramProvider.SendTextAsync($"Sedang menghapus {urlFeed}");

                var delete = await _rssService.DeleteRssAsync(urlFeed);

                var success = delete.ToBool()
                    ? "berhasil."
                    : "gagal. Mungkin RSS tersebut sudah di hapus atau belum di tambahkan";

                await _telegramProvider.EditAsync($"Hapus {urlFeed} {success}");
            }
        }
    }
}