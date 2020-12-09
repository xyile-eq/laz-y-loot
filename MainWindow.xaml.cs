using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace LazLootIni
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();
            ViewModel.UIDispatcher = this.Dispatcher;
            this.DataContext = viewModel;

            viewModel.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.LogText))
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtStatus.Focus();
                        txtStatus.CaretIndex = txtStatus.Text.Length;
                        txtStatus.ScrollToEnd();
                    });
                }
            };

            if (File.Exists(Properties.Settings.Default.DefaultLoadFile))
            {
                viewModel.LoadFile(Properties.Settings.Default.DefaultLoadFile, true);
            }
        }

        public ViewModel viewModel { get; set; }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileDrop"))
            {
                var files = e.Data.GetData("FileDrop") as string[];
                if (files != null && files.Length == 1)
                {
                    if (Properties.Settings.Default.DefaultLoadFile != files[0])
                    {
                        if (MessageBox.Show($"The file you just dropped is not set as the default. Would you like to set it now? Once set, this will automatically be loaded on launch.", "Set default?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            Properties.Settings.Default.DefaultLoadFile = files[0];
                            Properties.Settings.Default.Save();
                        }
                    }
                    viewModel.LoadFile(files[0], true);
                }
            }
        }

        public class ViewModel : INPC
        {
            private DispatcherTimer autoReloaderTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(30) };

            private Dictionary<string, CostInfo> costDictionary = new Dictionary<string, CostInfo>();

            static public Dispatcher UIDispatcher;

            public ViewModel()
            {
                LocalItemDB.LoadAsync().ContinueWith(t => LocalItemDB.Instance = new List<ItemIconInfo>(t.Result));

                var costInfo = JsonConvert.DeserializeObject<CostInfo[]>(File.ReadAllText(LocalItemDB.ItemValuesPath));
                costDictionary = costInfo.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());

                autoReloaderTimer.Tick += (o, e) =>
                {
                    if (loadInProgress)
                    {
                        return;
                    }

                    loadInProgress = true;
                    Task.Run(() =>
                    {
                        var removeItems = new List<ParsedItem>();
                        var addItems = new List<ParsedItem>();
                        var curSelection = SelectedItem;
                        if (File.Exists(CurrentFileName))
                        {
                            var newList = ParseItems(File.ReadAllLines(CurrentFileName));
                            foreach (var item in newList)
                            {
                                var curLootDict = AllLoot.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
                                if (!curLootDict.TryGetValue(item.Name, out var pi))
                                {
                                    // something new has arrived
                                    item.NewlyAdded = true;
                                    WriteToLog($"A new item was added: {item.Name}");
                                    AllLoot.Add(item);
                                }
                                else
                                {
                                    // we do know of it, so let's compare things
                                    if (item.IsSkip != pi.IsSkip || item.IsDestroy != pi.IsDestroy || item.IsLore != pi.IsLore || item.IsSell != pi.IsSell || item.IsKeep != pi.IsKeep || item.StackCount != pi.StackCount)
                                    {
                                        removeItems.Add(pi);
                                        addItems.Add(item);
                                        //AllLoot.Remove(pi);
                                        //AllLoot.Add(item);
                                    }
                                }
                            }

                            UIDispatcher.Invoke(() =>
                            {
                                foreach (var item in removeItems)
                                {
                                    AllLoot.Remove(item);
                                }
                                foreach (var item in addItems)
                                {
                                    AllLoot.Add(item);
                                }
                                loadInProgress = false;
                            });
                        }
                    });
                };
                autoReloaderTimer.Start();
                searchTimer.Tick += (o, e) =>
                {
                    NotifyPropertyChanged(nameof(AllLootCVS));
                };
            }

            private int _numberOfBackupsToKeep = Properties.Settings.Default.NumberBackupsRetain;
            public int NumberOfBackupsToKeep
            {
                get
                {
                    return _numberOfBackupsToKeep;
                }

                set
                {
                    _numberOfBackupsToKeep = value;
                    NotifyPropertyChanged(nameof(NumberOfBackupsToKeep));
                }
            }


            private string _logText;
            public string LogText
            {
                get
                {
                    return _logText;
                }

                set
                {
                    _logText = value;
                    NotifyPropertyChanged(nameof(LogText));
                }
            }

            private DispatcherTimer searchTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(400) };

            private string _searchText;
            public string SearchText
            {
                get
                {
                    return _searchText;
                }

                set
                {
                    _searchText = value;
                    NotifyPropertyChanged(nameof(SearchText));
                    searchTimer.Stop();
                    searchTimer.Start();
                    
                }
            }

            private ParsedItem _selectedItem;
            public ParsedItem SelectedItem
            {
                get
                {
                    return _selectedItem;
                }

                set
                {
                    _selectedItem = value;
                    NotifyPropertyChanged(nameof(SelectedItem));
                }
            }



            public void WriteToLog(string message)
            {
                LogText += $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {message}\n";
            }

            private string _currentFileName;
            public string CurrentFileName
            {
                get
                {
                    return _currentFileName;
                }

                set
                {
                    _currentFileName = value;
                    NotifyPropertyChanged(nameof(CurrentFileName));
                }
            }

            private ObservableCollection<ParsedItem> _allLoot = new ObservableCollection<ParsedItem>();
            public ObservableCollection<ParsedItem> AllLoot
            {
                get
                {
                    return _allLoot;
                }

                set
                {
                    _allLoot = value;
                    NotifyPropertyChanged(nameof(AllLoot));
                    NotifyPropertyChanged(nameof(AllLootCVS));
                }
            }

            private bool _FilterPlatNoSell;
            public bool FilterPlatNoSell
            {
                get
                {
                    return _FilterPlatNoSell;
                }

                set
                {
                    _FilterPlatNoSell = value;
                    NotifyPropertyChanged(nameof(FilterPlatNoSell));
                    if (FilterKeepNoValue)
                    {
                        FilterKeepNoValue = false;
                    }
                    if (value)
                    {
                        NotifyPropertyChanged(nameof(AllLootCVS));
                    }
                }
            }

            private bool _filterKeepNoValue;
            public bool FilterKeepNoValue
            {
                get
                {
                    return _filterKeepNoValue;
                }

                set
                {
                    _filterKeepNoValue = value;
                    NotifyPropertyChanged(nameof(FilterKeepNoValue));
                    if (FilterPlatNoSell)
                    {
                        FilterPlatNoSell = false;
                    }
                    if (value)
                    {
                        NotifyPropertyChanged(nameof(AllLootCVS));
                    }
                }
            }

            private bool _FilterRecentItems;
            public bool FilterRecentItems
            {
                get
                {
                    return _FilterRecentItems;
                }

                set
                {
                    _FilterRecentItems = value;
                    NotifyPropertyChanged(nameof(FilterRecentItems));
                    NotifyPropertyChanged(nameof(AllLootCVS));
                }
            }


            public ICollectionView AllLootCVS
            {
                get
                {
                    var cvs = new CollectionViewSource();
                    cvs.Source = AllLoot;

                    cvs.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

                    //cvs.GroupDescriptions.Add(new PropertyGroupDescription("Description"));

                    var anyFilter = false;

                    if (FilterPlatNoSell)
                    {
                        cvs.Filter += (o, e) =>
                        {
                            var pi = e.Item as ParsedItem;
                            e.Accepted = !pi.IsSell && pi.Value != null && pi.Value > 0;
                        };
                        anyFilter = true;
                    }
                    else if (FilterKeepNoValue)
                    {
                        cvs.Filter += (o, e) =>
                        {
                            var pi = e.Item as ParsedItem;
                            e.Accepted = pi.IsKeep && pi.Value == null || pi.Value < 1;
                        };
                        anyFilter = true;
                    }

                    if (FilterRecentItems)
                    {
                        cvs.Filter += (o, e) =>
                        {
                            var pi = e.Item as ParsedItem;
                            e.Accepted = pi.NewlyAdded;
                        };
                        anyFilter = true;
                    }

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        cvs.Filter += (o, e) =>
                        {
                            var pi = e.Item as ParsedItem;
                            e.Accepted = pi.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        };
                        anyFilter = true;
                    }

                    //if (!anyFilter)
                    //{
                    //    cvs.Filter += (o, e) => { e.Accepted = false; };
                    //}

                    var theList = cvs.View.Cast<ParsedItem>().ToList();
                    Task.Run(() =>
                    {
                        foreach (var ni in theList)
                        {
                            if (ni.ItemInfo == null && LocalItemDB.Instance != null)
                            {
                                ni.ItemInfo = LocalItemDB.Instance.FirstOrDefault(x => x.Name == ni.Name);
                            }

                            if (costDictionary.TryGetValue(ni.Name, out var ci))
                            {
                                ni.PlatValue = ci.Price / 1000;
                                ni.TributeValue = ci.Favor;
                            }
                        }
                    });

                    return cvs.View;
                }
            }

            private ObservableCollection<string> _loadedLines = new ObservableCollection<string>();
            public ObservableCollection<string> LoadedLines
            {
                get
                {
                    return _loadedLines;
                }

                set
                {
                    _loadedLines = value;
                    NotifyPropertyChanged(nameof(LoadedLines));
                }
            }

            public void TakeBackup(string path)
            {
                var fi = new FileInfo(path);
                var dfullname = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
                var backupPath = dfullname + @"\backups\";
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }
                var outPath = System.IO.Path.Combine(backupPath, fi.Name.Replace(fi.Extension, $"_backup-{DateTime.Now.Ticks}{fi.Extension}"));
                File.Copy(path, outPath);

                WriteToLog($"A snapshot of the loot file was taken for backup purposes. It is located at: {outPath}.");

                var files = Directory.GetFiles(backupPath).Select(x => new FileInfo(x));
                WriteToLog($"There are currently {files.Count()} backups. When more than {Properties.Settings.Default.NumberBackupsRetain} exist, the oldest will be deleted.");
                if (files.Count() > NumberOfBackupsToKeep)
                {
                    try
                    {
                        var toDelete = files.OrderBy(x => x.CreationTime).First();
                        WriteToLog($"Deleting oldest backup to make room. Name: {toDelete.FullName}");
                    }
                    catch (Exception ex)
                    {
                        WriteToLog($"An exception occurred while deleting the oldest backup: {ex.Message}");
                    }
                }
            }

            private List<ParsedItem> ParseItems(string[] lines)
            {
                var ret = new List<ParsedItem>();
                var currentIndex = 0;
                foreach (var line in lines)
                {
                    currentIndex++;
                    LoadedLines.Add(line);

                    if (line.StartsWith("["))
                    {
                        continue;
                    }

                    if (Regex.IsMatch(line, ". is for="))
                    {
                        continue;
                    }

                    var words = default(string[]);

                    try
                    {
                        words = line.Substring(0, line.IndexOf('=')).Split(' ');
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error on line {currentIndex}: {ex.Message}");
                        Console.WriteLine($"Line contents: {line}");
                        continue;
                    }

                    var valstack = words.Last();
                    if (valstack.IndexOf("(") >= 0 || Regex.Match(valstack, @"[0-9]{1,}p").Success)
                    {
                        words = words.Take(words.Length - 1).ToArray();
                    }

                    var actions = line.Substring(line.IndexOf('='));

                    var pi = new ParsedItem()
                    {
                        OriginalText = line,
                        LineIndex = currentIndex,
                        Name = string.Join(" ", words),
                        IsLore = valstack.IndexOf("(L)") >= 0,
                        IsKeep = actions.IndexOf("Keep") >= 0,
                        IsDestroy = actions.IndexOf("Destroy") >= 0,
                        IsSell = actions.IndexOf("Sell") >= 0,
                        IsSkip = actions.IndexOf("Skip") >= 0
                    };

                    if (int.TryParse(Regex.Match(valstack, @"\((?<stack>[0-9]*)\)").Groups["stack"].Value, out var stackval))
                    {
                        pi.StackCount = stackval;
                    }

                    if (int.TryParse(Regex.Match(valstack, @"(?<val>[0-9]{1,})p").Groups["val"].Value, out var vval))
                    {
                        pi.Value = vval;
                    }

                    pi.ItemChanged += (_, _) =>
                    {
                        var lineToUpdate = LoadedLines.IndexOf(pi.OriginalText);
                        if (lineToUpdate < 0)
                        {
                            MessageBox.Show($"Unable to find line to update using original text: {pi.OriginalText}");
                        }

                        // otherwise lets go ahead and replace
                        var newText = pi.GenerateLine();
                        LoadedLines[lineToUpdate] = newText;
                        pi.OriginalText = newText;
                        WriteToLog($"Updating line {lineToUpdate} to: {newText}");
                        File.Delete(CurrentFileName);
                        File.WriteAllLines(CurrentFileName, LoadedLines.ToArray());
                    };

                    pi.TriggerUpdates = true;

                    ret.Add(pi);
                }
                return ret;
            }

            Queue<ParsedItem> readyForDisplay = new Queue<ParsedItem>();
            private bool loadInProgress = false;

            public void LoadFile(string path, bool takeBackup = false)
            {
                CurrentFileName = path;

                if (takeBackup)
                {
                    try
                    {
                        TakeBackup(path);
                    }
                    catch (Exception ex)
                    {
                        WriteToLog($"An exception occurred while loading: {ex.Message}");
                    }
                }

                try
                {
                    var lines = File.ReadAllLines(path);
                    
                    loadInProgress = true;

                    var templist = new ObservableCollection<ParsedItem>();
                    Task.Run(() =>
                    {
                        foreach (var item in ParseItems(lines))
                        {
                            templist.Add(item);
                        }
                    }).ContinueWith((t) =>
                    {
                        AllLoot = templist;
                        loadInProgress = false;
                        WriteToLog($"A total of {AllLoot.Count} item(s) were loaded from the ini file.");
                    });
                }
                catch (Exception ex)
                {
                    WriteToLog($"An exception occurred while loading: {ex.Message}");
                }
            }



        }
    }
}
