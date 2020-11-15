using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Zizi.Bot.Common;
using Zizi.Bot.Services;
using Zizi.Bot.Tools;

namespace Zizi.Bot.Telegram
{
    public static class AdminUtil
    {
        private const string BaseCacheKey = "admin-group";

        public static async Task UpdateCacheAdminAsync(this TelegramService telegramService)
        {
            var client = telegramService.Client;
            var message = telegramService.Message;
            var chatId = message.Chat.Id;

            var admins = await client.GetChatAdministratorsAsync(chatId)
                .ConfigureAwait(false);

            telegramService.SetChatCache(BaseCacheKey, admins);
        }

        public static async Task UpdateCacheAdminAsync(this ITelegramBotClient client, long chatId)
        {
            var keyCache = $"{chatId}-{BaseCacheKey}";

            var admins = await client.GetChatAdministratorsAsync(chatId)
                .ConfigureAwait(false);

            admins.AddCache(keyCache);
        }

        public static async Task<ChatMember[]> GetChatAdmin(this TelegramService telegramService)
        {
            var message = telegramService.Message;
            // var client = telegramService.Client;
            // var chatId = message.Chat.Id;

            var cacheExist = telegramService.IsChatCacheExist(BaseCacheKey);
            if (!cacheExist)
            {
                await telegramService.UpdateCacheAdminAsync().ConfigureAwait(false);
                // var admins = await client.GetChatAdministratorsAsync(chatId)
                // .ConfigureAwait(false);

                // telegramService.SetChatCache(CacheKey, admins);
            }

            var chatMembers = telegramService.GetChatCache<ChatMember[]>(BaseCacheKey);
            // Log.Debug("ChatMemberAdmin: {0}", chatMembers.ToJson(true));

            return chatMembers;
        }

        public static async Task<ChatMember[]> GetChatAdmin(this ITelegramBotClient botClient, long chatId)
        {
            var keyCache = $"{chatId}-{BaseCacheKey}";

            var cacheExist = MonkeyCacheUtil.IsCacheExist(keyCache);
            if (!cacheExist)
            {
                await botClient.UpdateCacheAdminAsync(chatId).ConfigureAwait(false);
            }

            var chatMembers = MonkeyCacheUtil.Get<ChatMember[]>(keyCache);
            // Log.Debug("ChatMemberAdmin: {0}", chatMembers.ToJson(true));

            return chatMembers;
        }

        public static async Task<bool> IsAdminChat(this TelegramService telegramService, int userId = -1)
        {
            var sw = Stopwatch.StartNew();
            var isAdmin = false;
            var message = telegramService.AnyMessage;
            userId = userId == -1 ? message.From.Id : userId;

            if (telegramService.IsPrivateChat()) return false;

            var chatMembers = await telegramService.GetChatAdmin()
                .ConfigureAwait(false);

            foreach (var admin in chatMembers)
            {
                if (userId == admin.User.Id)
                {
                    isAdmin = true;
                    break;
                }
            }

            Log.Information("UserId {0} IsAdmin: {1}. Time: {2}", userId, isAdmin, sw.Elapsed);
            sw.Stop();

            return isAdmin;
        }

        public static async Task<bool> IsAdminChat(this ITelegramBotClient botClient, long chatId, int userId)
        {
            var sw = Stopwatch.StartNew();
            // var message = telegramService.Message;

            // if (telegramService.IsPrivateChat()) return false;

            var chatMembers = await botClient.GetChatAdmin(chatId)
                .ConfigureAwait(false);

            var isAdmin = chatMembers.Any(admin => userId == admin.User.Id);

            Log.Information("UserId {0} IsAdmin: {1}. Time: {2}", userId, isAdmin, sw.Elapsed);
            sw.Stop();

            return isAdmin;
        }
    }
}