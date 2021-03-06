﻿using static SmartLyrics.Common.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Type = SmartLyrics.Common.Logging.Type;

namespace SmartLyrics.WebRequests
{
    internal static class Spotify
    {
        public static async Task<string> GetSavedSongs(string authHeader, string url)
        {
            using HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", authHeader);

                Uri urlToSend = new Uri(url);

                HttpResponseMessage responseAsync = await client.GetAsync(urlToSend);

                Log(Type.Info, "Reading content stream...");
                await using Stream stream = await responseAsync.Content.ReadAsStreamAsync();
                using StreamReader reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (HttpRequestException e)
            {
                Log(Type.Error, "Exception caught while getting song list from Spotify!\n" + e);
                return null;
            }
        }
    }
}