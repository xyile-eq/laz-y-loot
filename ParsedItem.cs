using System;
using System.Collections.Generic;
using System.Web;

namespace LazLootIni
{
    public class ParsedItem : INPC
    {

        public event EventHandler<EventArgs> ItemChanged;

        public string OriginalText { get; set; }

        public int LineIndex { get; set; }


        public string LootedBy { get; set; }

        public string Name { get; set; }
        public bool IsLore { get; set; }

        public bool IsNoDrop { get; set; }

        public int? Value { get; set; }

        public int? PlatValue { get; set; }
        public int? TributeValue { get; set; }
        public int? StackCount { get; set; }

        private bool _triggerUpdates = false;
        public bool TriggerUpdates
        {
            get
            {
                return _triggerUpdates;
            }

            set
            {
                _triggerUpdates = value;
                NotifyPropertyChanged(nameof(TriggerUpdates));
            }
        }

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


        private bool _newlyAdded;
        public bool NewlyAdded
        {
            get
            {
                return _newlyAdded;
            }

            set
            {
                _newlyAdded = value;
                NotifyPropertyChanged(nameof(NewlyAdded));
            }
        }

        private bool _showAtTop;
        public bool ShowAtTop
        {
            get
            {
                return _showAtTop;
            }

            set
            {
                _showAtTop = value;
                NotifyPropertyChanged(nameof(ShowAtTop));
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
                if (value == true && TriggerUpdates)
                {
                    _isSkip = false;
                    NotifyPropertyChanged(nameof(IsSkip));

                    ItemChanged?.Invoke(this, new EventArgs());
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
                if (value == true && TriggerUpdates)
                {
                    _isKeep = false;
                    NotifyPropertyChanged(nameof(IsKeep));
                    ItemChanged?.Invoke(this, new EventArgs());
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
                if (value == true && TriggerUpdates)
                {
                    _isDestroy = false;
                    NotifyPropertyChanged(nameof(IsDestroy));
                    ItemChanged?.Invoke(this, new EventArgs());
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
                if (value == true && TriggerUpdates)
                {
                    _isSell = false;
                    NotifyPropertyChanged(nameof(IsSell));
                    ItemChanged?.Invoke(this, new EventArgs());
                }
            }
        }

        public string Description
        {
            get
            {
                var ret = new List<string>();

                if (IsKeep)
                {
                    ret.Add("Keep");
                }
                if (IsSkip)
                {
                    ret.Add("Skip");
                }
                if (IsSell)
                {
                    ret.Add("Sell");
                }
                if (IsDestroy)
                {
                    ret.Add("Destroy");
                }

                return string.Join(" and ", ret);
            }
        }

        private ItemIconInfo _itemInfo;
        public ItemIconInfo ItemInfo
        {
            get
            {
                return _itemInfo;
            }

            set
            {
                _itemInfo = value;
                NotifyPropertyChanged(nameof(ItemInfo));
            }
        }

        public void UpdateLine()
        {

            var newLine = GenerateLine();
            

        }


        public string GenerateLine()
        {
            var stk = StackCount != null && StackCount > 0 ? $"({StackCount})" : "";
            var val = Value < 1 || Value == null ? "" : $"{Value}p";
            var lore = IsLore ? "(L)" : "";
            var nd = IsNoDrop ? "(ND)" : "";

            var keepOrSkip = IsKeep ? "Keep" : "Skip";
            if (IsKeep && StackCount > 0)
            {
                keepOrSkip += $"|{StackCount}";
            }

            if (IsSell)
            {
                keepOrSkip += ",Sell";
            }
            else if (IsDestroy)
            {
                keepOrSkip += ",Destroy";
            }

            var output = Name;

            if (!string.IsNullOrEmpty(val) || !string.IsNullOrEmpty(lore) || !string.IsNullOrEmpty(stk))
            {
                output += " ";
            }
            return $"{output}{val}{nd}{lore}{stk}={keepOrSkip}";
        }

        private InlineCommand _checkBazCommand;

        public InlineCommand checkBazCommand
        {
            get
            {
                if (_checkBazCommand == null)
                {
                    _checkBazCommand = new InlineCommand((obj) =>
                    {
                        var url = $"http://www.lazaruseq.com/Magelo/index.php?page=bazaar&item={HttpUtility.UrlEncode(Name)}&trader=&class=-1&race=-1&slot=-1&stat=-1&aug_type=2147483647&type=-1&pricemin=&pricemax=";
                        System.Diagnostics.Process.Start(url);
                    });
                }

                return _checkBazCommand;
            }
        }
    }
}
