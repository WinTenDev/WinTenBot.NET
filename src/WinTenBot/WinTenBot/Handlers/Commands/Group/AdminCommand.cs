﻿using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types.Enums;
using WinTenBot.Helpers;
using WinTenBot.Providers;

namespace WinTenBot.Handlers.Commands.Group
{
    public class AdminCommand : CommandBase
    {
        private RequestProvider _requestProvider;

        public override async Task HandleAsync(IUpdateContext context, UpdateDelegate next, string[] args, CancellationToken cancellationToken)
        {
            var msg = context.Update.Message;
            _requestProvider = new RequestProvider(context);

            await _requestProvider.SendTextAsync("🍽 Loading..");
            //            var admins = context.Update.Message.Chat.AllMembersAreAdministrators;
            var admins = await context.Bot.Client.GetChatAdministratorsAsync(msg.Chat.Id, cancellationToken);
            var creatorStr = string.Empty;
            var adminStr = string.Empty;
            int number = 1;
            foreach (var admin in admins)
            {
                var user = admin.User;
                var nameLink = MemberHelper.GetNameLink(user.Id, (user.FirstName + " " + user.LastName).Trim());
                if (admin.Status == ChatMemberStatus.Creator)
                {
                    creatorStr = nameLink;
                }
                else
                {
                    adminStr += $"{number++}. {nameLink}\n";
                }

                //                Console.WriteLine(TextHelper.ToJson(admin));
                //                await chatProcessor.EditAsync(TextHelper.ToJson(admin));
            }

            var sendText = $"👤 <b>Creator</b>" +
                           $"\n└ {creatorStr}" +
                           $"\n" +
                           $"\n👥️ <b>Administrators</b>" +
                           $"\n{adminStr}";

            await _requestProvider.EditAsync(sendText);
        }
    }
}