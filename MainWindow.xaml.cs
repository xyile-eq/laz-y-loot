using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            this.DataContext = viewModel;

            viewModel.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.LogText))
                {
                    txtStatus.Focus();
                    txtStatus.CaretIndex = txtStatus.Text.Length;
                    txtStatus.ScrollToEnd();
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
    }
}
