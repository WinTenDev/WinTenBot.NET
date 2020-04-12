using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SqlKata;
using SqlKata.Execution;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using WinTenBot.Model;
using WinTenBot.Providers;
using WinTenBot.Services;

namespace WinTenBot.Helpers
{
    public static class MessageHelper
    {
        public static string GetFileId(this Message message)
        {
            var fileId = "";
            switch (message.Type)
            {
                case MessageType.Document:
                    fileId = message.Document.FileId;
                    break;

                case MessageType.Photo:
                    fileId = message.Photo[0].FileId;
                    break;

                case MessageType.Video:
                    fileId = message.Video.FileId;
                    break;
            }

            return fileId;
        }

        public static string GetReducedFileId(this Message message)
        {
            return GetFileId(message).Substring(0, 17);
        }

        public static string GetTextWithoutCmd(this string message, bool withoutCmd = true)
        {
            var partsMsg = message.Split(' ');
            var text = message;
            if (withoutCmd && message.StartsWith("/"))
            {
                text = message.TrimStart(partsMsg[0].ToCharArray());
            }

            return text.Trim();
        }

        public static bool IsNeedRunTasks(this TelegramProvider telegramProvider)
        {
            var message = telegramProvider.Message;
            
            return message.NewChatMembers == null 
                   || message.LeftChatMember == null
                   || !telegramProvider.IsPrivateChat();
        }

        public static string GetMessageLink(this Message message)
        {
            var chatUsername = message.Chat.Username;
            var messageId = message.MessageId;

            var messageLink = $"https://t.me/{chatUsername}/{messageId}";

            if (chatUsername == "")
            {
                var trimmedChatId = message.Chat.Id.ToString().Substring(4);
                messageLink = $"https://t.me/c/{trimmedChatId}/{messageId}";
            }

            Log.Information($"MessageLink: {messageLink}");
            return messageLink;
        }

        public static async Task<bool> IsMustDelete(string words)
        {
            var isMust = false;
            var query = await new Query("word_filter")
                .ExecForSqLite(true)
                .GetAsync();

            var mapped = query.ToJson().MapObject<List<WordFilter>>();

            var partedWord = words.Split(" ");
            foreach (var word in partedWord)
            {
                foreach (WordFilter wordFilter in mapped)
                {
                    var forFilter = wordFilter.Word;
                    var isGlobal = wordFilter.IsGlobal;

                    if (forFilter == word.ToLower())
                    {
                        isMust = true;
                    }

                    // Log.Debug($"Is ({isGlobal}) '{word}' == '{forFilter}' ? {isMust}");

                    if (isMust) break;
                }
            }

            return isMust;
        }

        public static async Task CheckMessageAsync(this TelegramProvider telegramProvider)
        {
            try
            {
                Log.Information("Starting check Message");

                var message = telegramProvider.MessageOrEdited;
                
                var settingService = new SettingsService(message);
                var chatSettings = await settingService.GetSettingByGroup();

                if (!chatSettings.EnableWordFilterGroupWide)
                {
                    Log.Information("Global Word Filter is disabled!");
                    return;
                }
                
                var text = message.Text;
                if (!text.IsNullOrEmpty())
                {
                    var isMustDelete = await IsMustDelete(text);
                    Log.Information($"Message {message.MessageId} IsMustDelete: {isMustDelete}");

                    if (isMustDelete) await telegramProvider.DeleteAsync(message.MessageId);
                }
                else
                {
                    Log.Information("No message Text for scan.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking message");
            }
        }

        public static async Task FindNotesAsync(this TelegramProvider telegramProvider)
        {
            try
            {
                Log.Information("Starting find Notes in Cloud");
                InlineKeyboardMarkup inlineKeyboardMarkup = null;
                
                var message = telegramProvider.MessageOrEdited;
                var settingService = new SettingsService(message);
                var chatSettings = await settingService.GetSettingByGroup();
                if (!chatSettings.EnableFindNotes)
                {
                    Log.Information("Find Notes is disabled in this Group!");
                    return;
                }
                
                var notesService = new NotesService();

                var selectedNotes = await notesService.GetNotesBySlug(message.Chat.Id, message.Text);
                if (selectedNotes.Count > 0)
                {
                    var content = selectedNotes[0].Content;
                    var btnData = selectedNotes[0].BtnData;
                    if (btnData != "")
                    {
                        inlineKeyboardMarkup = btnData.ToReplyMarkup(2);
                    }

                    await telegramProvider.SendTextAsync(content, inlineKeyboardMarkup);
                    inlineKeyboardMarkup = null;

                    foreach (var note in selectedNotes)
                    {
                        Log.Debug(note.ToJson());
                    }
                }
                else
                {
                    Log.Debug("No rows result set in Notes");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error when getting Notes");
            }
        }

        public static async Task FindTagsAsync(this TelegramProvider telegramProvider)
        {
            var message = telegramProvider.MessageOrEdited;
            var settingService = new SettingsService(message);
            var chatSettings = await settingService.GetSettingByGroup();
            if (!chatSettings.EnableFindTags)
            {
                Log.Information("Find Tags is disabled in this Group!");
                return;
            }

            var tagsService = new TagsService();
            if (!message.Text.Contains("#"))
            {
                Log.Information($"Message {message.MessageId} is not contains any Hashtag.");
                return;
            }

            Log.Information("Tags Received..");
            var partsText = message.Text.Split(new char[] {' ', '\n', ','})
                .Where(x => x.Contains("#")).ToArray();

            var allTags = partsText.Count();
            var limitedTags = partsText.Take(5).ToArray();
            var limitedCount = limitedTags.Count();

            Log.Information($"AllTags: {allTags.ToJson(true)}");
            Log.Information($"First 5: {limitedTags.ToJson(true)}");
            //            int count = 1;
            foreach (var split in limitedTags)
            {
                var trimTag = split.TrimStart('#');
                Log.Information($"Processing : {trimTag}");

                var tagData = await tagsService.GetTagByTag(message.Chat.Id, trimTag);
                Log.Information($"Data of tag: {trimTag} {tagData.ToJson(true)}");

                var content = tagData[0].Content;
                var buttonStr = tagData[0].BtnData;

                InlineKeyboardMarkup buttonMarkup = null;
                if (!buttonStr.IsNullOrEmpty())
                {
                    buttonMarkup = buttonStr.ToReplyMarkup(2);
                }

                await telegramProvider.SendTextAsync(content, buttonMarkup);
            }

            if (allTags > limitedCount)
            {
                await telegramProvider.SendTextAsync("Due performance reason, we limit 5 batch call tags");
            }
        }
    }
}