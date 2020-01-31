﻿using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using WinTenBot.Helpers;
using WinTenBot.Helpers.Processors;
using WinTenBot.Providers;
using WinTenBot.Services;

namespace WinTenBot.Handlers.Commands.Rules
{
    public class RulesCommand : CommandBase
    {
        private RequestProvider _requestProvider;
        private SettingsService _settingsService;

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args,
            CancellationToken cancellationToken)
        {
            var msg = context.Update.Message;

            _requestProvider = new RequestProvider(context);
            _settingsService = new SettingsService(msg);


            var sendText = "Under maintenance";
            if (msg.Chat.Type != ChatType.Private)
            {
                if (msg.From.Id.IsSudoer())
                {
                    var settings = await _settingsService.GetSettingByGroup();
                    await _settingsService.UpdateCache();
                    ConsoleHelper.WriteLine(settings.ToJson());
                    // var rules = settings.Rows[0]["rules_text"].ToString();
                    var rules = settings.RulesText;
                    ConsoleHelper.WriteLine(rules);
                    sendText = rules;
                }
            }
            else
            {
                sendText = "Rules hanya untuk grup";
            }

            await _requestProvider.SendTextAsync(sendText);
        }
    }
}