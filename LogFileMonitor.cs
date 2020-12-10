using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LazLootIni
{
    public class LootedEventArgs
    {
        public string Looter { get; set; }
        public string ItemName { get; set; }
    }

    public class LogFileMonitor : INPC
    {
        public LogFileMonitor(string path)
        {
            if (File.Exists(path))
            {
                LogPath = path;
                Worker = StartParse();
            }
        }

        private  BackgroundWorker Worker { get; set; }
        private FileStream fileStream { get; set; }
        private DispatcherTimer parseTimer { get; set; }

        private StreamReader streamReader { get; set; }

        private string _logPath = Properties.Settings.Default.LogPath;
        public string LogPath
        {
            get { return _logPath; }
            set
            {
                _logPath = value;
                NotifyPropertyChanged("LogPath");

                Properties.Settings.Default.LogPath = value;
                Properties.Settings.Default.Save();
            }
        }

        public event EventHandler<LootedEventArgs> OnLoot = (o, e) => { };

        public void HandleLine(string line)
        {
            if (line.Contains(']'))
            {
                line = line.Substring(line.IndexOf(']') + 2);
                var lootMatch = Regex.Match(line, @"--(?<name>[\w\W]*?) (has|have) looted a (?<itemName>[\w\W]*?).--");
                if (lootMatch.Success)
                {
                    OnLoot(this, new LootedEventArgs() { ItemName = lootMatch.Groups["itemName"].Value, Looter = lootMatch.Groups["name"].Value });
                    //Console.WriteLine($"item looted by {lootMatch.Groups["name"].Value}: {lootMatch.Groups["itemName"].Value}");
                }
            }
        }

        public BackgroundWorker StartParse()
        {
            fileStream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // start by going to the end of the log
            fileStream.Seek(0, SeekOrigin.End);
            streamReader = new StreamReader(fileStream);

            parseTimer = new DispatcherTimer();
            parseTimer.Interval = TimeSpan.FromMilliseconds(150);
            parseTimer.Tick += (ox, ex) =>
            {
                //if (e.Cancel)
                //    parseTimer.Stop();

                if (fileStream.Position < fileStream.Length)
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();

                        HandleLine(line);
                    }
                }
            };
            parseTimer.Start();

            var bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += (o, e) =>
            {

            };
            bw.RunWorkerAsync();
            return bw;
        }
    }
}
