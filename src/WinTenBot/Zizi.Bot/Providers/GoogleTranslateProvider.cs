﻿using System.Threading.Tasks;
using GoogleTranslateFreeApi;

namespace Zizi.Bot.Providers
{
    public static class GoogleTranslateProvider
    {
        public static async Task<TranslationResult> TranslateAsync(this string forTranslate, string toLang)
        {
            var translator = new GoogleTranslator();
            var from = Language.Auto;
            var to = GoogleTranslator.GetLanguageByISO(toLang);

            var result = await translator.TranslateAsync(forTranslate, from, to)
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<string> MergedTranslateAsync(this string forTranslate, string toLang)
        {
            return (await forTranslate.TranslateAsync(toLang).ConfigureAwait(false)).MergedTranslation;
        }
    }
}