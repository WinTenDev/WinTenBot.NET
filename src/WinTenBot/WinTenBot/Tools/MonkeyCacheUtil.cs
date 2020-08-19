﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonkeyCache;
using MonkeyCache.FileStore;
using Serilog;
using Telegram.Bot.Types;
using WinTenBot.IO;
using WinTenBot.Services;
using WinTenBot.Telegram;

namespace WinTenBot.Tools
{
    public static class MonkeyCacheUtil
    {
        public static void SetupCache()
        {
            Log.Information("Initializing MonkeyCache");
            var cachePath = Path.Combine("Storage", "MonkeyCache").SanitizeSlash().EnsureDirectory(true);
            Barrel.ApplicationId = "ZiziBot-Cache";
            BarrelUtils.SetBaseCachePath(cachePath);

            Log.Debug("MonkeyCache initialized.");
        }

        public static void Add<T>(string key, T data)
        {
            Log.Information("Adding Monkeys with key: {0}", key);
            Barrel.Current.Add(key, data, TimeSpan.FromDays(1));
        }

        public static T Get<T>(string key)
        {
            Log.Information("Getting Monkeys data: {0}", key);
            return Barrel.Current.Get<T>(key);
        }

        public static IEnumerable<string> GetKeys()
        {
            Log.Information("Getting all Monkeys Cache");
            var keys = Barrel.Current.GetKeys();
            Log.Debug("Monkeys Count: {0}", keys.Count());

            return keys;
        }

        public static T SetChatCache<T>(this Message msg, string key, T data)
        {
            // var msg = telegramService.Message;
            var chatId = msg.Chat.Id.ReduceChatId();
            var msgId = msg.MessageId;
            var keyCache = $"{chatId}-{key}";

            Add(keyCache, data);
            return data;
        }

        public static T SetChatCache<T>(this TelegramService telegramService, string key, T data)
        {
            var msg = telegramService.Message;
            msg.SetChatCache(key, data);
            
            // var chatId = msg.Chat.Id.ReduceChatId();
            // var msgId = msg.MessageId;
            // var keyCache = $"{chatId}-{key}";
            //
            // Add(keyCache, data);
            
            return data;
        }

        public static T GetChatCache<T>(this Message msg, string key)
        {
            // var msg = telegramService.Message;
            var chatId = msg.Chat.Id.ReduceChatId();
            var msgId = msg.MessageId;
            var keyCache = $"{chatId}-{key}";

            var data = Get<T>(keyCache);
            return data;
        }

        public static T GetChatCache<T>(this TelegramService telegramService, string key)
        {
            var msg = telegramService.Message;
            var data = msg.GetChatCache<T>(key);

            // var chatId = msg.Chat.Id.ReduceChatId();
            // var msgId = msg.MessageId;
            // var keyCache = $"{chatId}-{key}";

            // var data = Get<T>(keyCache);
            
            return data;
        }
    }
}