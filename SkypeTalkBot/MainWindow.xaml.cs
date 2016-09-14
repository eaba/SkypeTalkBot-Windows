using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ProgReporter;
using SKYPE4COMLib;

namespace SkypeTalkBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Show Window
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow = 1);
        // Focus Window
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hwnd);

        public static Skype Skype;
        public static ProgStats Stats;

        private bool _selfclose = false;

        /// <summary>
        /// Główne wejście aplikacji
        /// </summary>
        public MainWindow()
        {
            // Załadowanie kontrolek
            InitializeComponent();

            if (Environment.GetCommandLineArgs() != null)
            {
                if (Environment.GetCommandLineArgs().Any(arg => arg.Contains("getkey")))
                {
                    MessageBox.Show("Your beta key: " + ID.GetFingerprint());
                }
            }

            Thread thr = new Thread(delegate()
            {
                // Aktualizacja
                Updater.Update("SkypeTalkBot");
            })
            {
                IsBackground = true
            };
            thr.Start();

            // Wyślij statystyki
            Stats = new ProgStats();
            Stats.AppLicenseType = LicenseType.Free;
            Stats.AppVersion = Updater.GetActiveVersion();
            Stats.AppStart("aa53533c18a451bfb80749f96081aac4399ed59e");
        }

        /// <summary>
        /// Zamykanie okna
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_selfclose)
            {
                // Zakończ wysyłanie statystyk
                Stats.AppStop();
            }
        }

        /// <summary>
        /// Połącz ze Skype
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectToSkype(object sender, RoutedEventArgs e)
        {
            Thread thr = new Thread(delegate()
            {
                // Utwórz obiekt Skype
                Skype = new Skype();

                // Czy nie wykryto Skype?
                if (!Skype.Client.IsRunning)
                {
                    // Uruchom Skype
                    Skype.Client.Start();

                    // Pokaż powiadomienie
                    MessageBox.Show("Skype is not working.", "SkypeTalkBot", MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Zatrzymaj dalsze wykonywanie kodu
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(delegate
                {
                    // Wyłącz przycisk
                    (sender as Button).IsEnabled = false;

                    // Zmień wyświetlany tekst
                    (sender as Button).Content = "Connecting...";
                });

                Process[] processes = Process.GetProcessesByName("Skype");
                foreach (Process process in processes)
                {
                    ShowWindow(process.MainWindowHandle);
                    SetForegroundWindow(process.MainWindowHandle);
                }

                try
                {
                    // Połącz ze Skype
                    Skype.Attach(7, true);
                }
                catch
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(delegate
                    {
                        // Zmień wyświetlany tekst
                        (sender as Button).Content = "Access denied";

                        // Włącz przycisk
                        (sender as Button).IsEnabled = true;
                    });

                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(delegate
                {
                    // Zmień wyświetlany tekst
                    (sender as Button).Content = "Connected";

                    // Utworzenie obiektu okna
                    SettingsWindow sw = new SettingsWindow();

                    // Pokazanie okna
                    sw.Show();

                    // Zaznacz flagę zamykającego się okna
                    _selfclose = true;

                    // Ukrycie okna
                    Close();
                });
            })
            {
                IsBackground = true
            };
            thr.Start();
        }
    }
}
