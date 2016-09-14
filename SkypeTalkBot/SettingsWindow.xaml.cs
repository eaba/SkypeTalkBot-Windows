using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChatterBotAPI;
using SKYPE4COMLib;
using Application = System.Windows.Application;
using Settings = SkypeTalkBot.Properties.Settings;

namespace SkypeTalkBot
{
    /// <summary>
    ///     Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private const string _WELCOMEMESSAGE_TEMPLATE = "/me{0}{1}";
        private const string _WELCOMEMESSAGE_0 = " - SkypeTalkBot";
        private ChatterBot _bot;
        private readonly List<string> _botIds = new List<string>();

        private readonly List<ChatterBotSession> _botSessions = new List<ChatterBotSession>();
        private ChatterBotFactory _factory;
        private readonly List<string> _lastMsg = new List<string>();
        private int _messagesSent;
        private int _sessions;

        /// <summary>
        ///     Główne wejście aplikacji
        /// </summary>
        public SettingsWindow()
        {
            // Załadowanie kontrolek
            InitializeComponent();

            // Popraw układ kontrolek
            Width = 525;
            SettingsGrid.Margin = new Thickness(-519,0,0,0);
            MainGrid.Margin = new Thickness(0,0,0,0);

            // Załaduj ustawienia aplikacji
            welcomeMessageBox.Document.Blocks.Clear();
            welcomeMessageBox.Document.Blocks.Add(new Paragraph(new Run(Settings.Default.welcomeMessage)));
            welcomeMessageCheckbox.IsChecked = Settings.Default.sendWelcomeMessage;
            acceptFriendRequestsCheckbox.IsChecked = Settings.Default.acceptFriendRequests;
        }

        /// <summary>
        ///     Okno jest zamykane
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            // Zapisz ustawienia aplikacji
            Settings.Default.welcomeMessage =
                new TextRange(welcomeMessageBox.Document.ContentStart, welcomeMessageBox.Document.ContentEnd).Text
                    .Remove(
                        new TextRange(welcomeMessageBox.Document.ContentStart, welcomeMessageBox.Document.ContentEnd)
                            .Text.LastIndexOf("\r\n"), 2);
            Settings.Default.sendWelcomeMessage = (bool) welcomeMessageCheckbox.IsChecked;
            Settings.Default.acceptFriendRequests = (bool)acceptFriendRequestsCheckbox.IsChecked;
            Settings.Default.Save();

            // Zakończ wysyłanie statystyk
            MainWindow.Stats.AppStop();
        }

        /// <summary>
        ///     Włącz/Wyłącz bota
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleBot(object sender, RoutedEventArgs e)
        {
            // Czy {_botThread} jest uruchomiony?
            if (_factory != null)
            {
                // Usuń event otrzymania wiadomości
                MainWindow.Skype.MessageStatus -= Skype_MessageRecieved;

                // Oczyszczanie pamięci
                _factory = null;
                _bot = null;
                _botSessions.Clear();
                _botIds.Clear();
                _lastMsg.Clear();
                _sessions = 0;
                _messagesSent = 0;

                // Zaktualizuj przycisk
                var resourceUri = new Uri("Resources/Toggle Off-96.png", UriKind.Relative);
                var streamInfo = Application.GetResourceStream(resourceUri);
                var temp = BitmapFrame.Create(streamInfo.Stream);
                var brush = new ImageBrush();
                brush.ImageSource = temp;

                (sender as Button).Background = brush;

                // Zaktualizuj statystyki
                statisticsSessions.Text = _sessions.ToString();
                statisticsSent.Text = _messagesSent.ToString();
            }
            else
            {
                // Uruchom bot-a
                EnableBot();

                // Zaktualizuj przycisk
                var resourceUri = new Uri("Resources/Toggle On-96.png", UriKind.Relative);
                var streamInfo = Application.GetResourceStream(resourceUri);
                var temp = BitmapFrame.Create(streamInfo.Stream);
                var brush = new ImageBrush();
                brush.ImageSource = temp;

                (sender as Button).Background = brush;
            }
        }

        /// <summary>
        ///     Obsługa bota
        /// </summary>
        private void EnableBot()
        {
            // Utwórz obiekt bota
            _factory = new ChatterBotFactory();
            _bot = _factory.Create(ChatterBotType.CLEVERBOT);

            // Event otrzymania wiadomości
            MainWindow.Skype.MessageStatus += Skype_MessageRecieved;

            var events = (_ISkypeEvents_Event)MainWindow.Skype;
            events.UserAuthorizationRequestReceived += Skype_FriendRequestRecieved;
        }

        /// <summary>
        ///     Otrzymanie wiadomości
        /// </summary>
        /// <param name="pMessage"></param>
        /// <param name="Status"></param>
        private async void Skype_MessageRecieved(ChatMessage pMessage, TChatMessageStatus Status)
        {
            try
            {
                // Czy ta wiadomość została wysłana?
                if (pMessage.Sender.Handle == MainWindow.Skype.CurrentUser.Handle)
                    return;

                // Wartość {newSession}
                // * Pomocnicza
                var newSession = false;

                // ! Czy sesja jest już uruchomiona?
                if (!IsSessionOpen(pMessage.Sender.Handle))
                {
                    // Utwórz nową sesję
                    _botSessions.Add(_bot.CreateSession());

                    // Utwórz ID bota
                    _botIds.Add(pMessage.Sender.Handle);

                    // Utwórz historię wiadomości
                    _lastMsg.Add(pMessage.Body);

                    // Wywołaj w wątku UI
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        // Czy wysłać wiadomość powitalną?
                        if ((bool)welcomeMessageCheckbox.IsChecked)
                        {
                            // Utwórz wiadomość powitalną
                            var welcomeMessageResponse = _WELCOMEMESSAGE_TEMPLATE.Replace("{0}", _WELCOMEMESSAGE_0);

                            // Pobierz wiadomość powitalną użytkownika
                            var welcomeMessageText =
                                new TextRange(welcomeMessageBox.Document.ContentStart, welcomeMessageBox.Document.ContentEnd)
                                    .Text;

                            // ! Czy wiadomość powitalna użytkownika jest pusta?
                            if (!string.IsNullOrWhiteSpace(welcomeMessageText))
                                welcomeMessageText = "\n" + welcomeMessageText;

                            // Obsługa specjalnego tekstu
                            welcomeMessageText = welcomeMessageText.Replace("{name}", pMessage.Sender.FullName);
                            welcomeMessageText = welcomeMessageText.Replace("{username}", pMessage.Sender.Handle);

                            // Dodaj wiaomość powitalną użytkownika
                            welcomeMessageResponse = welcomeMessageResponse.Replace("{1}", welcomeMessageText);

                            // Wyślij wiadomość powitalną
                            MainWindow.Skype.SendMessage(pMessage.Sender.Handle, welcomeMessageResponse);
                        }

                        newSession = true;

                        // Zwiększ ilość sesji
                        _sessions++;

                        // Zaktualizuj statystyki
                        statisticsSessions.Text = _sessions.ToString();
                    });
                }

                // Pobierz ID sesji
                var sessionsIndex = GetSessionIndex(pMessage.Sender.Handle);

                // Anty-spam
                if ((_lastMsg[sessionsIndex] == pMessage.Body) && !newSession)
                    return;

                // Zaktualizuj anty-spam
                _lastMsg[sessionsIndex] = pMessage.Body;

                var response = string.Empty;

                await Task.Run(delegate
                {
                    // Przeanalizuj odpowiedź bota
                    response = _botSessions[sessionsIndex].Think(pMessage.Body);
                });

                // Wyślij odpowiedź
                MainWindow.Skype.SendMessage(pMessage.Sender.Handle, response);

                if (_factory == null)
                {
                    return;
                }

                // Zwiększ ilość wysłanych wiadomośći
                _messagesSent++;

                // Wywołaj w wątku UI
                Application.Current.Dispatcher.Invoke(delegate
                {
                    // Zaktualizuj statystyki
                    statisticsSent.Text = _messagesSent.ToString();
                });
            }
            catch
            { }
        }

        /// <summary>
        /// Akceptowanie zaproszeń do znajomych
        /// </summary>
        /// <param name="pUser"></param>
        private void Skype_FriendRequestRecieved(User pUser)
        {
            // Czy funkcja jest włączona?
            if((bool)acceptFriendRequestsCheckbox.IsChecked && _factory != null)
            {
                // Dodaj do znajomych
                pUser.IsAuthorized = true;
            }
        }

        /// <summary>
        ///     Animacja przycisków
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMouseEnter(object sender, MouseEventArgs e)
        {
            // Uchwyt do obiektu
            var obj = sender as Button;

            // Pobierz wartość {Margin}
            var margin = obj.Margin;

            // Zmień wartość {Margin}
            obj.Margin = new Thickness(margin.Left - 1, margin.Top - 1, margin.Right, margin.Bottom);

            obj.Width = obj.Width + 2;
            obj.Height = obj.Height + 2;
        }

        /// <summary>
        ///     Zakończenie animacji
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMouseLeave(object sender, MouseEventArgs e)
        {
            // Uchwyt do obiektu
            var obj = sender as Button;

            // Pobierz wartość {Margin}
            var margin = obj.Margin;

            // Zmień wartość {Margin}
            obj.Margin = new Thickness(margin.Left + 1, margin.Top + 1, margin.Right, margin.Bottom);

            obj.Width = obj.Width - 2;
            obj.Height = obj.Height - 2;
        }

        /// <summary>
        /// Przełącz stronę
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchPage(object sender, RoutedEventArgs e)
        {
            // Pobierz nazwę obiektu
            var name = (sender as Button).Name.Replace("Switch", "").Replace("Grid", "");

            // Zdecyduj którą funkcję należy wykonać
            switch (name)
            {
                // Przełącz na okno główne
                case "Settings":
                    Thread thr = new Thread(delegate ()
                    {
                        var move = 0;
                        var done = false;

                        while (true)
                        {
                            Application.Current.Dispatcher.Invoke(delegate
                            {
                                // Czy gridy zostały przesunięte?
                                if (MainGrid.Margin.Left - move <= 0)
                                {
                                    // Popraw pozycję gridów
                                    SettingsGrid.Margin = new Thickness(-519, 0, 0, 0);
                                    MainGrid.Margin = new Thickness(0, 0, 0, 0);

                                    // Możliwość zakończenia wątku
                                    done = true;
                                }
                                else
                                {
                                    // Przesuń gridy w lewo
                                    SettingsGrid.Margin = new Thickness(SettingsGrid.Margin.Left - move, 0, 0, 0);
                                    MainGrid.Margin = new Thickness(MainGrid.Margin.Left - move, 0, 0, 0);
                                }
                            });

                            // Czy należy zakończyć wątek?
                            if (done)
                            {
                                break;
                            }

                            // Zwiększ szybkość przesuwania
                            move++;

                            // Opóznienie
                            Thread.Sleep(15);
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thr.Start();
                    break;
                // Przełącz na okno ustawień
                case "Main":
                    Thread thr2 = new Thread(delegate ()
                    {
                        var move = 1;
                        var done = false;

                        while (true)
                        {
                            Application.Current.Dispatcher.Invoke(delegate
                            {
                                // Czy gridy zostały przesunięte?
                                if (SettingsGrid.Margin.Left + move >= 0)
                                {
                                    // Popraw pozycję gridów
                                    SettingsGrid.Margin = new Thickness(0, 0, 0, 0);
                                    MainGrid.Margin = new Thickness(519, 0, 0, 0);

                                    // Możliwość zakończenia wątku
                                    done = true;
                                }
                                else
                                {
                                    // Przesuń gridy w prawo
                                    SettingsGrid.Margin = new Thickness(SettingsGrid.Margin.Left + move, 0, 0, 0);
                                    MainGrid.Margin = new Thickness(MainGrid.Margin.Left + move, 0, 0, 0);
                                }
                            });

                            // Czy należy zakończyć wątek?
                            if (done)
                            {
                                break;
                            }

                            // Zwiększ szybkość przesuwania
                            move++;

                            // Opóznienie
                            Thread.Sleep(15);
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thr2.Start();
                    break;
            }
        }

        /// <summary>
        ///     Czy sesja jest już uruchomiona?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool IsSessionOpen(string id)
        {
            for (var i = 0; i < _botIds.Count; i++)
                if (_botIds[i] == id)
                    return true;

            return false;
        }

        /// <summary>
        ///     Pobierz ID sesji
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private int GetSessionIndex(string id)
        {
            for (var i = 0; i < _botIds.Count; i++)
                if (_botIds[i] == id)
                    return i;

            return 0;
        }
    }
}