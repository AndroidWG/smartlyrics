﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Transformations;
using static SmartLyrics.Globals;
using Object = Java.Lang.Object;

namespace SmartLyrics.Common
{
    #region Classes
    public class SongBundle
    {
        public Song Normal { get; set; }
        public RomanizedSong Romanized { get; set; }

        public SongBundle(Song song, RomanizedSong romanized)
        {
            Normal = song;
            Romanized = romanized;
        }

        public SongBundle()
        {
            Normal = new Song();
            Romanized = new RomanizedSong();
        }
    }

    //TODO: Split into different classes used when displaying, saving, and comparing to notification song
    public class Song
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty; //TODO: Change Song class "artist" property to the Artist class
        public string Album { get; set; } = string.Empty;
        public string FeaturedArtist { get; set; } = string.Empty;
        public string Cover { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public string ApiPath { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Lyrics { get; set; } = string.Empty;
        public bool Romanized { get; set; } //used when saving to a file
        public int Likeness { get; set; } //used by the NLService
        public int Id { get; set; }

        private static RomanizedSong ToRomanizedSong(Song song)
        {
            RomanizedSong s = new RomanizedSong
            {
                Title = song.Title,
                Artist = song.Artist,
                Album = song.Album,
                FeaturedArtist = song.FeaturedArtist,
                Id = song.Id
            };

            return s;
        }

        //Conversions here are explicit to make sure a RomanizedSong,
        //which has incomplete Genius data, accidentally gets auto-converted
        //and starts causing problems.
        public static implicit operator RomanizedSong(Song s)
        {
            return ToRomanizedSong(s);
        }
    }

    public class RomanizedSong
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string FeaturedArtist { get; set; } = string.Empty;
        public string Lyrics { get; set; } = string.Empty;
        public int Id { get; set; }

        private static Song ToSong(RomanizedSong song)
        {
            Song s = new Song
            {
                Title = song.Title,
                Artist = song.Artist,
                Album = song.Album,
                FeaturedArtist = song.FeaturedArtist,
                Id = song.Id
            };

            return s;
        }

        //Conversions here are explicit to make sure a RomanizedSong,
        //which has incomplete Genius data, accidentally gets auto-converted
        //and starts causing problems.
        public static explicit operator Song(RomanizedSong rs)
        {
            return ToSong(rs);
        }
    }

    public class Artist
    {
        public string Name { get; set; }
        public string RomanizedName { get; set; }
        public List<SongBundle> Songs { get; set; }
    }
    #endregion

    #region Adapters
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public class ExpandableListAdapter : BaseExpandableListAdapter
    {
        private readonly Activity context;
        private readonly List<Artist> listDataHeader;
        private readonly Dictionary<Artist, List<SongBundle>> listDataChild;
        //private readonly List<string> filteredHeader;
        //private readonly Dictionary<string, List<string>> filteredChild;

        public ExpandableListAdapter(Activity context, List<Artist> listDataHeader, Dictionary<Artist, List<SongBundle>> listChildData)
        {
            listDataChild = listChildData;
            this.listDataHeader = listDataHeader;
            this.context = context;
        }
        //for child item view
        public override Object GetGroup(int groupPosition)
        {
            return null;
        }
        public override Object GetChild(int groupPosition, int childPosition)
        {
            SongBundle song = listDataChild[listDataHeader[groupPosition]][childPosition];

            if (Prefs.GetBoolean("auto_romanize_details", true) && song.Romanized != null)
            {
                return song.Romanized.Title;
            }

            return song.Normal.Title;
        }
        public override long GetChildId(int groupPosition, int childPosition)
        {
            return childPosition;
        }

        public override View GetChildView(int groupPosition, int childPosition, bool isLastChild, View convertView, ViewGroup parent)
        {
            string childText = (string)GetChild(groupPosition, childPosition);
            convertView ??= context.LayoutInflater.Inflate(Resource.Layout.list_child, null);
            
            TextView txtListChild = (TextView)convertView.FindViewById(Resource.Id.listChild);
            txtListChild.Text = childText;
            return convertView;
        }
        public override int GetChildrenCount(int groupPosition)
        {
            return listDataChild[listDataHeader[groupPosition]].Count;
        }
        //For header view
        public override int GroupCount => listDataHeader.Count;

        public override long GetGroupId(int groupPosition)
        {
            return groupPosition;
        }
        public override View GetGroupView(int groupPosition, bool isExpanded, View convertView, ViewGroup parent)
        {
            Artist artist = listDataHeader[groupPosition];
            string headerTitle = Prefs.GetBoolean("auto_romanize_details", false) && !string.IsNullOrEmpty(artist.RomanizedName)
                ? artist.RomanizedName
                : artist.Name;

            convertView ??= context.LayoutInflater.Inflate(Resource.Layout.list_header, null);
            TextView lblListHeader = (TextView)convertView.FindViewById(Resource.Id.listHeader);
            lblListHeader.Text = headerTitle;

            return convertView;
        }
        public override bool HasStableIds => false;

        public override bool IsChildSelectable(int groupPosition, int childPosition)
        {
            return true;
        }
    }

    public class SavedLyricsAdapter : BaseAdapter<Artist>
    {
        private readonly Activity activity;
        private readonly List<SongBundle> allSongs;

        public override Artist this[int position] => throw new NotImplementedException();

        public SavedLyricsAdapter(Activity activity, List<SongBundle> allSongs)
        {
            this.activity = activity;
            this.allSongs = allSongs;
        }

        public override int Count => allSongs.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.list_non_grouped, parent, false);
            TextView titleTxt = view.FindViewById<TextView>(Resource.Id.songTitle);
            TextView artistTxt = view.FindViewById<TextView>(Resource.Id.songArtist);

            SongBundle song = allSongs.ElementAt(position);

            if (Prefs.GetBoolean("auto_romanize_details", true) && song.Romanized != null)
            {
                artistTxt.Text = song.Romanized.Artist;
                titleTxt.Text = song.Romanized.Title;
            }
            else
            {
                artistTxt.Text = song.Normal.Artist;
                titleTxt.Text = song.Normal.Title;
            }

            return view;
        }
    }

    public class SearchResultAdapter : BaseAdapter<Song>
    {
        private readonly Activity activity;
        private readonly List<SongBundle> songs;

        public override Song this[int position] => throw new NotImplementedException();

        public SearchResultAdapter(Activity activity, List<SongBundle> songs)
        {
            this.activity = activity;
            this.songs = songs;
        }

        public override int Count => songs.Count;

        public override long GetItemId(int position)
        {
            return songs[position].Normal.Id;
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.list_item, parent, false);
            TextView titleTxt = view.FindViewById<TextView>(Resource.Id.songTitle);
            TextView artistTxt = view.FindViewById<TextView>(Resource.Id.songArtist);
            ImageView coverImg = view.FindViewById<ImageView>(Resource.Id.cover);

            SongBundle song = songs[position];
            
            if (Prefs.GetBoolean("auto_romanize_details", true) && song.Romanized != null)
            {
                artistTxt.Text = song.Romanized.Artist;
                titleTxt.Text = song.Romanized.Title;
            }
            else
            {
                artistTxt.Text = song.Normal.Artist;
                titleTxt.Text = song.Normal.Title;
            }
            ImageService.Instance.LoadUrl(song.Normal.Cover).Transform(new RoundedTransformation(20)).Into(coverImg);

            return view;
        }
    }
    #endregion
}