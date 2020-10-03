﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Serilog;
using Telegram.Bot.Types;
using Zizi.Bot.Common;
using Zizi.Bot.Models;
using Zizi.Bot.Tools;

namespace Zizi.Bot.Providers
{
    public static class AntiSpamProvider
    {
        public static async Task<SpamWatch> CheckSpamWatch(this int userId)
        {
            var spamWatch = new SpamWatch();
            var spamWatchToken = BotSettings.SpamWatchToken;

            try
            {
                var baseUrl = $"https://api.spamwat.ch/banlist/{userId}";
                spamWatch = await baseUrl
                    .WithOAuthBearerToken(spamWatchToken)
                    .GetJsonAsync<SpamWatch>()
                    .ConfigureAwait(false);
                spamWatch.IsBan = spamWatch.Code != 404;
                Log.Debug("SpamWatch Result: {0}", spamWatch.ToJson(true));
            }
            catch (FlurlHttpException ex)
            {
                var callHttpStatus = ex.Call.HttpStatus;
                Log.Information("StatusCode: {0}", callHttpStatus);
                switch (callHttpStatus)
                {
                    case HttpStatusCode.NotFound:
                        spamWatch.IsBan = false;
                        break;
                    case HttpStatusCode.Unauthorized:
                        Log.Warning("Please check your SpamWatch API Token!");
                        Log.Error(ex, "SpamWatch API FlurlHttpEx");
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SpamWatch Exception");
            }

            return spamWatch;
        }

        public static async Task<bool> CheckGBan(this int userId)
        {
            // var query = await new Query("fban_user")
            //     .Where("user_id", userId)
            //     .ExecForSqLite(true)
            //     .GetAsync()
            //     .ConfigureAwait(false);

            var jsonGBan = "gban-users".OpenJson();

            Log.Debug("Opening GBan collection");
            var gBanCollection = await jsonGBan.GetCollectionAsync<GlobalBanData>().ConfigureAwait(false);

            var allBan = gBanCollection.AsQueryable().ToList();
            Log.Debug("Loaded ES2 Ban: {0}", allBan.Count);

            var findBan = allBan.Where(x => x.UserId == userId).ToList();

            var isGBan = findBan.Any();
            Log.Information("UserId {0} is ES2 GBan? {1}", userId, isGBan);
            
            jsonGBan.Dispose();
            allBan.Clear();

            return isGBan;
        }

        public static async Task<bool> IsCasBanAsync(this User user)
        {
            try
            {
                var userId = user.Id;
                var url = "https://api.cas.chat/check".SetQueryParam("user_id", userId);
                var resp = await url.GetJsonAsync<CasBan>()
                    .ConfigureAwait(false);

                Log.Debug("CasBan Response", resp);

                var isBan = resp.Ok;
                Log.Information($"UserId: {userId} is CAS ban: {isBan}");
                return isBan;
            }
            catch
            {
                return false;
            }
        }
    }
}