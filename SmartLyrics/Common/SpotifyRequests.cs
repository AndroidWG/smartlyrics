﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace SmartLyrics
{
    class Spotify
    {
        public static async Task<string> GetSavedSongs(string authHeader, string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    Android.Util.Log.WriteLine(Android.Util.LogPriority.Verbose, "SmartLyrics", "SpotifyRequests.cs: Adding Auth headers to HttpClient");
                    client.DefaultRequestHeaders.Add("Authorization", authHeader);
                    Android.Util.Log.WriteLine(Android.Util.LogPriority.Warn, "SmartLyrics", "Url sent to HttpClient: " + new Uri(url));
                    HttpResponseMessage responseAsync = await client.GetAsync(new Uri(url));

                    Android.Util.Log.WriteLine(Android.Util.LogPriority.Verbose, "SmartLyrics", "SpotifyRequests.cs: Reading content stream...");
                    using (Stream stream = await responseAsync.Content.ReadAsStreamAsync())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (HttpRequestException e)
                {
                    Android.Util.Log.WriteLine(Android.Util.LogPriority.Error, "SmartLyrics", "SpotifyRequests.cs: Exception Caught:" + e.Message);
                    return null;
                }
            }
        }
    }
}