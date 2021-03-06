﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using Plugin.CurrentActivity;

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Timers;

using static SmartLyrics.Globals;
using static SmartLyrics.Common.Logging;
using Type = SmartLyrics.Common.Logging.Type;

namespace SmartLyrics
{
    [Activity(Label = "SpotifyDownload", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, ScreenOrientation = ScreenOrientation.Portrait)]
    public class SpotifyDownload : AppCompatActivity
    {
        private static readonly int RandomState = new Random().Next(2000, 10000000);
        private static string accessToken = "";
        private bool shouldStop;
        private readonly Timer timer = new Timer();
        private ConstraintLayout startedLayout;
        private ConstraintLayout finishedLayout;
        private Services.DownloadServiceConnection serviceConnection;
        private readonly string path = System.IO.Path.Combine(ApplicationPath, "SmartLyrics/Saved Lyrics/Spotify/");
        private readonly string pathImg = System.IO.Path.Combine(ApplicationPath, "SmartLyrics/Saved Lyrics/Spotify/Image Cache/");

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.main_spotify);
            CrossCurrentActivity.Current.Activity = this; //don't remove this, permission stuff needs it

            timer.Interval = 250;
            timer.Elapsed += CheckWebView;
            timer.Enabled = true;

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ImageButton drawerBtn = FindViewById<ImageButton>(Resource.Id.drawerBtn);

            WebView spotifyAuth = FindViewById<WebView>(Resource.Id.spotifyAuth);
            Button startBtn = FindViewById<Button>(Resource.Id.startBtn);
            ConstraintLayout infoLayout = FindViewById<ConstraintLayout>(Resource.Id.infoLayout);
            startedLayout = FindViewById<ConstraintLayout>(Resource.Id.startedLayout);
            finishedLayout = FindViewById<ConstraintLayout>(Resource.Id.finishedLayout);

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.NavigationItemSelected += NavigationView_NavigationViewSelected;

            Log(Type.Info, "Loaded view");

            serviceConnection = new Services.DownloadServiceConnection(this);

            if (serviceConnection.IsConnected)
            {
                infoLayout.Visibility = ViewStates.Gone;
                startedLayout.Visibility = ViewStates.Visible;
            }

            drawerBtn.Click += delegate
            {
                drawer.OpenDrawer(navigationView);
            };

            spotifyAuth.Settings.JavaScriptEnabled = true;
            spotifyAuth.SetWebViewClient(new SpotifyAuthClient(this));
            spotifyAuth.LoadUrl("https://accounts.spotify.com/authorize?client_id=cfa0924c4430409b9a90ad42cb9da301&redirect_uri=http:%2F%2Fredirect.test%2Freturn&scope=user-library-read&response_type=token&state=" + RandomState);
            Log(Type.Info, "Started LoadURL");

            startBtn.Click += async delegate
            {
                infoLayout.Visibility = ViewStates.Gone;
                startedLayout.Visibility = ViewStates.Visible;

                if (Directory.Exists(path))
                {
                    Log(Type.Info, "/SmartLyrics/Saved Lyrics directory exists!");
                }
                else
                {
                    Directory.CreateDirectory(path);
                    Log(Type.Info, "/SmartLyrics/Saved Lyrics directory doesn't exist, creating...");
                }

                if (Directory.Exists(pathImg))
                {
                    Log(Type.Info, "/SmartLyrics/Saved Lyrics/ImageCache directory exists!");
                }
                else
                {
                    Directory.CreateDirectory(pathImg);
                    Log(Type.Info, "/SmartLyrics/Saved Lyrics/ImageCache directory doesn't exist, creating...");
                }

                timer.Interval = 1000;
                timer.Elapsed += UpdateProgressBar;
                Log(Type.Info, "Setted up timer");

                Intent intent = new Intent(this, typeof(Services.DownloadService));
                intent.PutExtra("AccessToken", accessToken);
                Log(Type.Info, "Added extra to intent");
                StartForegroundService(intent);
                BindService(intent, serviceConnection, Bind.AutoCreate);
            };
        }

        private async void CheckWebView(object sender, ElapsedEventArgs e)
        {
            WebView spotifyAuth = FindViewById<WebView>(Resource.Id.spotifyAuth);
            ConstraintLayout infoLayout = FindViewById<ConstraintLayout>(Resource.Id.infoLayout);

            if (shouldStop)
            {
                //do nothing
            }
            else
            {
                if (accessToken != "")
                {
                    RunOnUiThread(() =>
                    {
                        spotifyAuth.Visibility = ViewStates.Gone;
                        infoLayout.Visibility = ViewStates.Visible;
                    });

                    shouldStop = true;
                }

            }
        }

        private async void UpdateProgressBar(object sender, ElapsedEventArgs e)
        {
            ProgressBar progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);

            Log(Type.Info, "" + serviceConnection.Binder.Service.GetProgress());

            if (serviceConnection.Binder.Service.GetProgress() != 100)
            {
                progressBar.Progress = serviceConnection.Binder.Service.GetProgress();
            }
            else
            {
                RunOnUiThread(() =>
                {
                    startedLayout.Visibility = ViewStates.Gone;
                    finishedLayout.Visibility = ViewStates.Visible;
                });

                timer.Stop();
            }
        }

        private void NavigationView_NavigationViewSelected(object sender, NavigationView.NavigationItemSelectedEventArgs e)
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            e.MenuItem.SetCheckable(true);
            Intent intent = new Intent(this, typeof(SpotifyDownload)).SetFlags(ActivityFlags.ReorderToFront);

            switch (e.MenuItem.ItemId)
            {
                case (Resource.Id.nav_search):
                    intent = new Intent(this, typeof(MainActivity)).SetFlags(ActivityFlags.ReorderToFront);
                    StartActivity(intent);
                    break;
                case (Resource.Id.nav_saved):
                    intent = new Intent(this, typeof(SavedLyrics)).SetFlags(ActivityFlags.ReorderToFront);
                    StartActivity(intent);
                    break;
                case (Resource.Id.nav_spotify):
                    drawer.CloseDrawers();
                    break;
                case (Resource.Id.nav_settings):
                    intent = new Intent(this, typeof(SettingsActivity)).SetFlags(ActivityFlags.ReorderToFront);
                    StartActivity(intent);
                    break;
            }

            e.MenuItem.SetCheckable(false);
            drawer.CloseDrawers();
        }

        public class SpotifyAuthClient : WebViewClient
        {
            private readonly Context context;

            public SpotifyAuthClient(Context context)
            {
                this.context = context;
            }

            public override bool ShouldOverrideUrlLoading(WebView view, string url)
            {
                view.LoadUrl(url);
                return true;
            }

            public override void OnPageStarted(WebView view, string url, Bitmap favicon)
            {
                Log(Type.Info, "Page started - " + url);

                if (url.Contains("http://redirect.test") && url.Contains(RandomState.ToString()))
                {
                    accessToken = Regex.Match(url, @"(?<=access_token=)(.*?)(?=&token_type)").ToString();
                    Log(Type.Info, "Page returned with access token!");

                    base.OnPageStarted(view, url, favicon);
                }

                base.OnPageStarted(view, url, favicon);
            }
        }
    }
}