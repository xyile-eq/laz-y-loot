using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LazLootIni
{
    /// <summary>
    /// Interaction logic for ItemBrowserWindow.xaml
    /// </summary>
    public partial class ItemBrowserWindow : Window
    {
        public ItemBrowserWindow()
        {
            InitializeComponent();
            viewModel = new ItemBrowserViewModel();
            this.DataContext = viewModel;
            viewModel.CloseDialogRequested += (o, e) => { this.Close(); };
        }

        public ItemBrowserViewModel viewModel { get; set; }
    }

    public class SearchItem : INPC
    {
        public ItemIconInfo ItemInfo { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }

            set
            {
                _isSelected = value;
                NotifyPropertyChanged(nameof(IsSelected));
            }
        }

    }

    public class ItemBrowserViewModel : INPC
    {
        private DispatcherTimer searchTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(400) };

        public ItemBrowserViewModel()
        {
            searchTimer.Tick += SearchTimer_Tick;
            Task.Run(() =>
            {
                foreach (var item in LocalItemDB.Instance)
                {
                    SearchResults.Add(new SearchItem()
                    {
                        ItemInfo = item,
                    });
                }
            }).ContinueWith((result) =>
            {
                SearchAvailable = true;
            });
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            SearchAvailable = false;
            NotifyPropertyChanged(nameof(SearchResultsCVS));
            searchTimer.Stop();
        }

        private bool _searchAvailable;
        public bool SearchAvailable
        {
            get
            {
                return _searchAvailable;
            }

            set
            {
                _searchAvailable = value;
                NotifyPropertyChanged(nameof(SearchAvailable));
            }
        }


        private string _ItemSearchText;
        public string ItemSearchText
        {
            get
            {
                return _ItemSearchText;
            }

            set
            {
                _ItemSearchText = value;
                NotifyPropertyChanged(nameof(ItemSearchText));
                searchTimer.Stop();
                searchTimer.Start();
            }
        }

        private List<SearchItem> _searchResults = new List<SearchItem>();
        public List<SearchItem> SearchResults
        {
            get
            {
                return _searchResults;
            }

            set
            {
                _searchResults = value;
                NotifyPropertyChanged(nameof(SearchResults));
            }
        }

        public ICollectionView SearchResultsCVS
        {
            get
            {
                var cvs = new CollectionViewSource();

                if (string.IsNullOrEmpty(ItemSearchText))
                {
                    return cvs.View;
                }

                cvs.Source = SearchResults;

                cvs.SortDescriptions.Add(new SortDescription("ItemInfo.Name", ListSortDirection.Ascending));

                cvs.Filter += (o, e) =>
                {
                    var item = e.Item as SearchItem;
                    if (string.IsNullOrEmpty(ItemSearchText) || ItemSearchText.Length < 3)
                    {
                        e.Accepted = false;
                    } 
                    else
                    {
                        e.Accepted = item.ItemInfo.Name.IndexOf(ItemSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                };

                SearchAvailable = true;

                return cvs.View;
            }
        }

        private bool _isKeep;
        public bool IsKeep
        {
            get
            {
                return _isKeep;
            }

            set
            {
                _isKeep = value;
                NotifyPropertyChanged(nameof(IsKeep));
                if (value == true)
                {
                    _isSkip = false;
                    NotifyPropertyChanged(nameof(IsSkip));
                }
            }
        }

        private bool _isSkip;
        public bool IsSkip
        {
            get
            {
                return _isSkip;
            }

            set
            {
                _isSkip = value;
                NotifyPropertyChanged(nameof(IsSkip));
                if (value == true)
                {
                    _isKeep = false;
                    NotifyPropertyChanged(nameof(IsKeep));
                }
            }
        }

        private bool _isSell;
        public bool IsSell
        {
            get
            {
                return _isSell;
            }

            set
            {
                _isSell = value;
                NotifyPropertyChanged(nameof(IsSell));
                if (value == true)
                {
                    _isDestroy = false;
                    NotifyPropertyChanged(nameof(IsDestroy));
                }
            }
        }
        private bool _isDestroy;
        public bool IsDestroy
        {
            get
            {
                return _isDestroy;
            }

            set
            {
                _isDestroy = value;
                NotifyPropertyChanged(nameof(IsDestroy));
                if (value == true)
                {
                    _isSell = false;
                    NotifyPropertyChanged(nameof(IsSell));
                }
            }
        }

        public event EventHandler CloseDialogRequested;


        private InlineCommand _cancelCommand;

        public InlineCommand cancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new InlineCommand((obj) =>
                    {
                        CloseDialogRequested?.Invoke(this, new EventArgs());
                    });
                }

                return _cancelCommand;
            }
        }

        

        private InlineCommand _addSelectedCommand;

        public InlineCommand addSelectedCommand
        {
            get
            {
                if (_addSelectedCommand == null)
                {
                    _addSelectedCommand = new InlineCommand((obj) =>
                    {
                        var selectedItems = SearchResults.Where(x => x.IsSelected);
                        if (selectedItems.Count() < 1)
                        {
                            MessageBox.Show("Please select atleast one item.");
                            return;
                        }
                        if (MessageBox.Show($"You are about to add {selectedItems.Count()} item(s) to your ini. Is this ok?", $"Confirm add", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {

                        }
                    });
                }

                return _addSelectedCommand;
            }
        }

    }

    public class AddItemEventArgs : EventArgs
    {
        public SearchItem[] ItemsToAdd { get; set; }

        public bool IsKeep { get; set; }
        public bool IsSkip { get; set; }
        public bool IsSell { get; set; }
        public bool IsDestroy { get; set; }
    }
}
