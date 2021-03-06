﻿using Android.Util;
using Mono.Data.Sqlite;
using SmartLyrics.Toolbox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static SmartLyrics.Globals;

namespace SmartLyrics.Common
{
    internal static class Logging
    {
        private static SqliteConnection sql;
        private static string filepath = "";

/*
        public enum Priority
        {
            Debug,
            Verbose,
            Warn,
            Error,
            Fatal
        }
*/

        public enum Type
        {
            Action,
            Event,
            Error,
            Processing,
            Info,
            Fatal
        }

        public static async Task StartSession()
        {
            await MiscTools.CheckAndCreateAppFolders();

            string loggingFolder = Path.Combine(ApplicationPath, LogsLocation);
            List<DateTime> previousSessions = new List<DateTime>();
            DateTime timestamp = DateTime.UtcNow;
            
            foreach (string s in Directory.EnumerateFiles(loggingFolder))
            {
                string timestampString = Path.GetFileNameWithoutExtension(s);
                if (DateTime.TryParseExact(timestampString, LogDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                {
                    previousSessions.Add(parsed);
                }
            }

            List<DateTime> orderByDescending = previousSessions.OrderByDescending(x => x).ToList();
            if (orderByDescending.Count != 0)
            {
                DateTime latest = previousSessions.First();
                string latestPath = Path.Combine(ApplicationPath, LogsLocation, latest.ToString(LogDateTimeFormat, CultureInfo.InvariantCulture) + LogDatabaseExtension);

                //If the latest log file is newer than 2 hours AND smaller than 5MB, use that file
                if (latest > DateTime.UtcNow.AddHours(LogFileMaxAge) || new FileInfo(latestPath).Length >= LogFileMaxSize)
                {
                    timestamp = latest;
                }
            }
            
            filepath = Path.Combine(ApplicationPath, LogsLocation, timestamp.ToString(LogDateTimeFormat, CultureInfo.InvariantCulture) + LogDatabaseExtension);
            InitializeDb();
        }

        private static void InitializeDb()
        {
            try
            {
                string source = "URI=file:" + filepath;
                sql = new SqliteConnection(source);
                sql.Open();

                using SqliteCommand cmd = new SqliteCommand(sql)
                {
                    CommandText = "DROP TABLE IF EXISTS log"
                };
                
                cmd.ExecuteNonQuery();
                cmd.CommandText = @"CREATE TABLE log(id INTEGER PRIMARY KEY,time DATETIME DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%f', 'NOW')),file TEXT,method TEXT,type TEXT,message TEXT,attach TEXT)";
                cmd.ExecuteNonQuery();
            }
            catch (ArgumentException)
            {
                //TODO: Add a log cache to log errors when the Logging Database is unavailable
                
                filepath = Path.Combine(ApplicationPath, LogsLocation, DateTime.UtcNow.ToString(LogDateTimeFormat, CultureInfo.InvariantCulture) + LogDatabaseExtension);
                InitializeDb();
            }
        }

        public static void Log(Type type,
                               string message,
                               string attachment = "",
                               [CallerMemberName] string memberName = "",
                               [CallerFilePath] string sourceFilePath = "",
                               [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (sql == null)
            {
                StartSession();
            }

            string file = Path.GetFileName(sourceFilePath.Replace('\\', '/'));
            string line = ", " + sourceLineNumber;
            string fileAndLine = file + line;

            LogPriority priority = type switch
            {
                Type.Action => LogPriority.Info,
                Type.Event => LogPriority.Debug,
                Type.Error => LogPriority.Warn,
                Type.Processing => LogPriority.Verbose,
                Type.Info => LogPriority.Verbose,
                Type.Fatal => LogPriority.Error,
                _ => LogPriority.Debug
            };
            //Exclude Processing log messages since they're mostly useless and spammy
            if (type != Type.Processing) { Android.Util.Log.WriteLine(priority, fileAndLine, memberName + ": " + message); }

            using SqliteCommand cmd = new SqliteCommand(sql)
            {
                CommandText =
                    $"INSERT INTO log(time, file, method, type, message, attach) VALUES(@Timestamp,@File,@Method,@Type,@Message,@Attach)"
            };
            cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            cmd.Parameters.AddWithValue("@File", file + line);
            cmd.Parameters.AddWithValue("@Method", memberName);
            cmd.Parameters.AddWithValue("@Type", type.ToString("G"));
            cmd.Parameters.AddWithValue("@Message", message);
            cmd.Parameters.AddWithValue("@Attach", attachment);
            cmd.ExecuteNonQuery();
        }

        public static FileInfo GetLatestLog()
        {
            DirectoryInfo loggingFolder = new DirectoryInfo(Path.Combine(ApplicationPath, LogsLocation));
            FileInfo latest = loggingFolder.GetFiles("*" + LogDatabaseExtension)
             .OrderByDescending(f => f.LastWriteTime)
             .ElementAt(1); //Gets the second most recent log file; most recent would be the one being currently used

            return latest;
        } 
    }
}