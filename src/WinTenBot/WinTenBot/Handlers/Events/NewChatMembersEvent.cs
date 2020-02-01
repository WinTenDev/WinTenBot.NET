﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot.Framework.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenBot.Helpers;
using WinTenBot.Helpers.Processors;
using WinTenBot.Model;
using WinTenBot.Providers;
using WinTenBot.Services;

namespace WinTenBot.Handlers.Events
{
    public class NewChatMembersEvent : IUpdateHandler
    {
        private SettingsService _settingsService;
        private RequestProvider _requestProvider;
        private ElasticSecurityService _elasticSecurityService;


        public NewChatMembersEvent()
        {
            
        }

        public async Task HandleAsync(IUpdateContext context, UpdateDelegate next, CancellationToken cancellationToken)
        {
            Message msg = context.Update.Message;
            _requestProvider = new RequestProvider(context);
            _settingsService = new SettingsService(msg);
            _elasticSecurityService = new ElasticSecurityService(context.Update.Message);

            ConsoleHelper.WriteLine("New Chat Members...");

            var newMembers = msg.NewChatMembers;
            var isBootAdded = await newMembers.IsBotAdded();
            if (isBootAdded)
            {
                // if (!msg.Chat.Id.IsAllowed())
                // {
                //     Log.Information("I must leave right now!");
                //     
                //     var msgOut = $"Sepertinya saya salah alamat, saya pamit dulu..";
                //
                //     await _requestProvider.SendTextAsync(msgOut);
                //     await _requestProvider.LeaveChat(msg.Chat.Id);
                //     return;
                // }

                var isRestricted = await _requestProvider.EnsureChatRestriction();
                if(isRestricted) return;

                var botName = Bot.GlobalConfiguration["Engines:ProductName"];
                var sendText = $"Hai, perkenalkan saya {botName}" +
                               $"\nFYI saya di bangun ulang menggunakan .NET Core, tepatnya ASP .NET Core." +
                               $"\n\nAku adalah bot pendebug dan grup manajemen yang di lengkapi dengan alat keamanan. " +
                               $"Agar saya berfungsi penuh, jadikan saya admin dengan level standard. " +
                               $"\n\nAku akan menerapkan konfigurasi standard jika aku baru pertama kali masuk kesini. " +
                               $"\n\nUntuk melihat daftar perintah bisa ketikkan /help";
                
                await _requestProvider.SendTextAsync(sendText);
                await _settingsService.SaveSettingsAsync(new Dictionary<string, object>()
                {
                    {"chat_id", msg.Chat.Id},
                    {"chat_title", msg.Chat.Title}
                });
                
                if (newMembers.Length == 1) return;
            }
            
            var parsedNewMember = await ParseMemberCategory(newMembers);
            var allNewMember = parsedNewMember.AllNewMember;
            var allNoUsername = parsedNewMember.AllNoUsername;
            var allNewBot = parsedNewMember.AllNewBot;
            
            if (allNewMember.Length > 0)
            {
                var chatSettings = await _settingsService.GetSettingByGroup();

                var chatTitle = msg.Chat.Title;
                var memberCount = await _requestProvider.GetMemberCount();
                var newMemberCount = newMembers.Length;

                ConsoleHelper.WriteLine("Preparing send Welcome..");

                if (chatSettings.WelcomeMessage.IsNullOrEmpty())
                {
                    chatSettings.WelcomeMessage = $"Hai {allNewMember}" +
                                                  $"\nSelamat datang di kontrakan {chatTitle}" +
                                                  $"\nKamu adalah anggota ke-{memberCount}";
                }

                var sendText = chatSettings.WelcomeMessage.ResolveVariable(new
                {
                    allNewMember,
                    allNoUsername,
                    allNewBot,
                    newMemberCount,
                    chatTitle,
                    memberCount
                });

                InlineKeyboardMarkup keyboard = null;
                if (!chatSettings.WelcomeButton.IsNullOrEmpty())
                {
                    keyboard = chatSettings.WelcomeButton.ToReplyMarkup(2);
                }

                if (!chatSettings.WelcomeMediaType.IsNullOrEmpty())
                {
                    await _requestProvider.SendMediaAsync(
                        chatSettings.WelcomeMedia,
                        chatSettings.WelcomeMediaType,
                        sendText,
                        keyboard);
                }
                else
                {
                    await _requestProvider.SendTextAsync(sendText, keyboard);
                }

                await _settingsService.SaveSettingsAsync(new Dictionary<string, object>()
                {
                    {"chat_id", msg.Chat.Id},
                    {"chat_title", msg.Chat.Title},
                    {"members_count", memberCount}
                });
            }
            else
            {
                ConsoleHelper.WriteLine("Welcome Message ignored because User is Global Banned.");
            }
        }

        private async Task<NewMember> ParseMemberCategory(User[] users)
        {
            var lastMember = users.Last();
            var newMembers = new NewMember();
            var allNewMember = new StringBuilder();
            var allNoUsername = new StringBuilder();
            var allNewBot = new StringBuilder();

            ConsoleHelper.WriteLine($"Parsing new {users.Length} members..");
            foreach (var newMember in users)
            {
                var isBan = await CheckGlobalBanAsync(newMember);
                if (isBan) continue;

                if (Bot.HostingEnvironment.IsProduction())
                {
                    // var isCasBan = await IsCasBan(newMember.Id);
                    await newMember.IsCasBanAsync();
                }

                var fullName = (newMember.FirstName + " " + newMember.LastName).Trim();
                var nameLink = MemberHelper.GetNameLink(newMember.Id, fullName);

                if (newMember != lastMember)
                {
                    allNewMember.Append(nameLink + ", ");
                }
                else
                {
                    allNewMember.Append(nameLink);
                }

                if (newMember.Username == "")
                {
                    allNoUsername.Append(nameLink);
                }

                if (newMember.IsBot)
                {
                    allNewBot.Append(nameLink);
                }
            }

            newMembers.AllNewMember = allNewMember;
            newMembers.AllNoUsername = allNoUsername;
            newMembers.AllNewBot = allNewBot;

            return newMembers;
        }

        private async Task<bool> CheckGlobalBanAsync(User user)
        {
            var userId = user.Id;
            var isKicked = false;

            var isBan = await _elasticSecurityService.IsExistInCache(userId);
            ConsoleHelper.WriteLine($"{user} IsBan: {isBan}");
            if (!isBan) return isKicked;

            var sendText = $"{user} terdeteksi pada penjaringan WinTenDev ES2 tapi gagal di tendang.";
            isKicked = await _requestProvider.KickMemberAsync(user);
            if (isKicked)
            {
                await _requestProvider.UnbanMemberAsync(user);
                sendText = sendText.Replace("tapi gagal", "dan berhasil");
            }
            else
            {
                sendText += " Pastikan saya admin yang dapat menghapus Pengguna";
            }

            await _requestProvider.SendTextAsync(sendText);

            return isKicked;
        }
    }
}