using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UpdaterNamespace
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private readonly string _appName;

        public UpdateWindow(string appName, bool forceUpdate, string activeVersion, string updateVersion)
        {
            InitializeComponent();

            _appName = appName;

            // Pobierz changelog
            ChangelogBox.Text = Updater.GetChangelog(_appName);

            VersionChangeLabel.Text = VersionChangeLabel.Text.Replace("{0}", activeVersion)
                .Replace("{1}", updateVersion);

            if (forceUpdate)
            {
                // Wymuś aktualizację
                UpdateButton_Click(UpdateButton, null);
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Czy trwa aktualizacja?
            if (!UpdateButton.IsEnabled)
            {
                // Anuluj zamykanie okna
                e.Cancel = true;
            }
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Updater.IsInternetAvailible())
            {
                MessageBox.Show("Unable to connect to the internet", _appName, MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

            // Zmodyfikuj wygląd okna
            (sender as Button).IsEnabled = false;
            ProgressLabel.Visibility = Visibility.Visible;

            var fileArray = Updater.GetUpdateData(_appName);

            await Task.Run(delegate
            {
                // Pobierz aktualizację
                Updater.DownloadUpdate(ProgressBar, ProgressLabel, fileArray, _appName);
            });

            // Zainstaluj aktualizację
            Updater.InstallUpdate(fileArray);

            // Wyłącz aplikację
            Application.Current.Shutdown();
        }
    }
}
