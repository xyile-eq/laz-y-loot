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

        public string OriginalLine { get; set; }
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

            lootBlockTimer.Tick += (o, e) =>
            {
                if (IsBlockingOwnLoots)
                {
                    IsBlockingOwnLoots = false;
                    lootBlockTimer.Stop();
                }
            };
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

        private bool _isBlockingOwnLoots = false;
        public bool IsBlockingOwnLoots
        {
            get
            {
                return _isBlockingOwnLoots;
            }

            set
            {
                _isBlockingOwnLoots = value;
                NotifyPropertyChanged(nameof(IsBlockingOwnLoots));
            }
        }

        private DispatcherTimer lootBlockTimer = new DispatcherTimer() { Interval = TimeSpan.FromMinutes(1) };


        public event EventHandler<LootedEventArgs> OnLoot = (o, e) => { };

        public void HandleLine(string line)
        {
            if (line.Contains(']'))
            {
                line = line.Substring(line.IndexOf(']') + 2);

                if (Regex.IsMatch(line, @"^You have been slain by") ||
                    Regex.IsMatch(line, @"(?i)tells you, 'wait4rez'$") ||
                    Regex.IsMatch(line, @"^You regain [0-9]{1,} experience from resurrection."))
                {
                    IsBlockingOwnLoots = true;
                    lootBlockTimer.Stop();
                    lootBlockTimer.Start();
                }

                var lootMatch = Regex.Match(line, @"--(?<name>[\w\W]*?) (has|have) looted a (?<itemName>[\w\W]*?).--");
                if (lootMatch.Success)
                {
                    var args = new LootedEventArgs() { ItemName = lootMatch.Groups["itemName"].Value, Looter = lootMatch.Groups["name"].Value, OriginalLine = line };

                    if (args.Looter == "You" && IsBlockingOwnLoots)
                    {
                        return;
                    }

                    OnLoot(this, args);
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
