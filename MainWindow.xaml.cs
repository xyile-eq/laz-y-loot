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
                    var tar = files[0];
                    var name = new FileInfo(tar).Name;
                    if (name.StartsWith("eqlog") && name.EndsWith(".txt"))
                    {
                        viewModel.LoadLog(new FileInfo(tar).FullName);
                    }
                    else
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
        }

        

        private void Window_Activated(object sender, EventArgs e)
        {
            viewModel.PerformRefreshIfNeeded();
        }
    }
}
