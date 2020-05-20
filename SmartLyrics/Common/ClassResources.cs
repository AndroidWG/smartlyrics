﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using FFImageLoading;
using FFImageLoading.Transformations;

namespace SmartLyrics
{
    public class Song
    {
        public string title { get; set; }
        public string artist { get; set; }
        public string album { get; set; }
        public string featuredArtist { get; set; }
        public string cover { get; set; }
        public string header { get; set; }
        public string APIPath { get; set; }
        public string path { get; set; }
        public string lyrics { get; set; }
        public int id { get; set; }
    }

    public class Artist
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<string> songs { get; set; }
    }

    public class ExpandableListAdapter : BaseExpandableListAdapter
    {
        private Activity context;
        private List<string> listDataHeader;
        private Dictionary<string, List<string>> listDataChild;
        private List<string> filteredHeader;
        private Dictionary<string, List<string>> filteredChild;

        public ExpandableListAdapter(Activity context, List<string> listDataHeader, Dictionary<String, List<string>> listChildData)
        {
            this.listDataChild = listChildData;
            this.listDataHeader = listDataHeader;
            this.context = context;
        }
        //for child item view
        public override Java.Lang.Object GetChild(int groupPosition, int childPosition)
        {
            return listDataChild[listDataHeader[groupPosition]][childPosition];
        }
        public override long GetChildId(int groupPosition, int childPosition)
        {
            return childPosition;
        }

        public override View GetChildView(int groupPosition, int childPosition, bool isLastChild, View convertView, ViewGroup parent)
        {
            string childText = (string)GetChild(groupPosition, childPosition);
            if (convertView == null)
            {
                convertView = context.LayoutInflater.Inflate(Resource.Layout.list_child, null);
            }
            TextView txtListChild = (TextView)convertView.FindViewById(Resource.Id.listChild);
            txtListChild.Text = childText;
            return convertView;
        }
        public override int GetChildrenCount(int groupPosition)
        {
            return listDataChild[listDataHeader[groupPosition]].Count;
        }
        //For header view
        public override Java.Lang.Object GetGroup(int groupPosition)
        {
            return listDataHeader[groupPosition];
        }
        public override int GroupCount {
            get {
                return listDataHeader.Count;
            }
        }
        public override long GetGroupId(int groupPosition)
        {
            return groupPosition;
        }
        public override View GetGroupView(int groupPosition, bool isExpanded, View convertView, ViewGroup parent)
        {
            string headerTitle = (string)GetGroup(groupPosition);

            convertView = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.list_header, null);
            var lblListHeader = (TextView)convertView.FindViewById(Resource.Id.listHeader);
            lblListHeader.Text = headerTitle;

            return convertView;
        }
        public override bool HasStableIds {
            get {
                return false;
            }
        }

        public override bool IsChildSelectable(int groupPosition, int childPosition)
        {
            return true;
        }
    }

    public class SavedLyricsAdapter : BaseAdapter<Artist>
    {
        private Activity activity;
        List<Tuple<string, string>> allSongs;

        public override Artist this[int position] => throw new NotImplementedException();

        public SavedLyricsAdapter(Activity activity, List<Tuple<string, string>> allSongs)
        {
            this.activity = activity;
            this.allSongs = allSongs;
        }

        public override int Count {
            get { return allSongs.Count; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.list_non_grouped, parent, false);
            var titleTxt = view.FindViewById<TextView>(Resource.Id.songTitle);
            var artistTxt = view.FindViewById<TextView>(Resource.Id.songArtist);

            titleTxt.Text = allSongs.ElementAt(position).Item2;
            artistTxt.Text = allSongs.ElementAt(position).Item1;
            return view;
        }
    }


    public class SearchResultAdapter : BaseAdapter<Song>
    {
        private Activity activity;
        private List<Song> songs;

        public override Song this[int position] => throw new NotImplementedException();

        public SearchResultAdapter(Activity activity, List<Song> songs)
        {
            this.activity = activity;
            this.songs = songs;
        }

        public override int Count {
            get { return songs.Count; }
        }

        public override long GetItemId(int position)
        {
            return songs[position].id;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.list_item, parent, false);
            var titleTxt = view.FindViewById<TextView>(Resource.Id.songTitle);
            var artistTxt = view.FindViewById<TextView>(Resource.Id.songArtist);
            var coverImg = view.FindViewById<ImageView>(Resource.Id.cover);

            titleTxt.Text = songs[position].title;
            artistTxt.Text = songs[position].artist;
            ImageService.Instance.LoadUrl(songs[position].cover).Transform(new RoundedTransformation(20)).Into(coverImg);

            return view;
        }
    }
}