using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UpdaterNamespace;

public static class Updater
{
    /*
     * Przykładowy wygląd pliku data:
     * beta=1.2.0.1
     * release=1.2.0.0
     * minimum=1.2.0.0
     * 
     * Prykładowy wyhląd pliku betatesters
     * zaczero=12345678
     * kasjan=ABCDEFGH
     */

    private static readonly string SERVER_URI = @"http://zcode.cba.pl/applications/";
    private static readonly string TEMP_PATH = Path.GetTempPath();
    private static readonly string STARTUP_PATH = Assembly.GetExecutingAssembly().Location;
    private static readonly string BETA_ID = "beta";
    private static readonly string RELEASE_ID = "release";
    private static readonly string MINIMUM_ID = "minimum";
    private static readonly string DATA_ID = "data";
    private static readonly string CHANGELOG_ID = "changelog";
    private static readonly string BETA_TESTERS_ID = "betatesters";

    public static int UPDATE_PROGRESS = 0;
    public static bool INCLUDE_BETA = false;

    /// <summary>
    /// Główna funkcja aktualizacji
    /// </summary>
    /// <param name="appName">Nazwa aplikacji</param>
    /// <param name="includeBeta">Czy brać pod uwagę pliki beta?</param>
    public static void Update(string appName)
    {
        if (!IsInternetAvailible()) return;

        try
        {
            // Sprawdź aktualizację, pokaż okno itp.
            INCLUDE_BETA = IsBetaTester(appName);
            var fileArray = GetUpdateData(appName);
            var isUpdateAvailible = IsUpdateAvailible(fileArray, appName);
            var isForceUpdateAvailible = IsForceUpdateAvailible(fileArray);

            // Czy dostępna jest aktualizcaja?
            if (isUpdateAvailible)
            {
                UpdateWindow window = new UpdateWindow(appName, isForceUpdateAvailible, GetActiveVersion(),
                    GetUpdateVersion(fileArray, appName));
                window.ShowDialog();
            }
        }
        catch
        {
            MessageBox.Show(
                "Update check failed.\nCheck our fanpage for updates:\nhttps://www.facebook.com/ZCodeApps/", "ZCode Updater", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Sprawdź dostępność internetu
    /// </summary>
    /// <returns>Czy internet jest dostępny?</returns>
    public static bool IsInternetAvailible()
    {
        try
        {
            using (var client = new WebClient())
            {
                using (var stream = client.OpenRead("http://zcode.cba.pl"))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Czy użytkownik ma uprawnienia do otrzymywania aktualizacji beta?
    /// </summary>
    /// <param name="appName"></param>
    /// <returns></returns>
    public static bool IsBetaTester(string appName)
    {
        var downloadFrom = SERVER_URI + appName + @"/" + BETA_TESTERS_ID;
        var downloadTo = TEMP_PATH + appName + "_" + BETA_TESTERS_ID;

        // Czy plik istnieje?
        if (File.Exists(downloadTo))
        {
            // Usuń plik
            File.Delete(downloadTo);
        }

        // Utwórz obiekt WebClient i pobierz dane z serwera
        WebClient wc = new WebClient();
        wc.DownloadFile(downloadFrom, downloadTo);

        // Oczyszczanie pamięci
        wc.Dispose();
        downloadFrom = null;

        var fingerPrint = ID.GetFingerprint();

        // Przeanalizuj każdą linię pliku
        foreach (var line in File.ReadLines(downloadTo))
        {
            // Czy fingerPrint znajduje się w bazie?
            if (line.Split('=')[1].Contains(fingerPrint))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Pobierz informacje changelogu
    /// </summary>
    /// <param name="appName">Nazwa aplikacji</param>
    /// <returns>Changelog</returns>
    public static string GetChangelog(string appName)
    {
        var downloadFrom = SERVER_URI + appName + @"/" + CHANGELOG_ID;
        var downloadTo = TEMP_PATH + appName + "_" + CHANGELOG_ID;

        // Czy plik istnieje?
        if (File.Exists(downloadTo))
        {
            // Usuń plik
            File.Delete(downloadTo);
        }

        // Utwórz obiekt WebClient i pobierz dane z serwera
        WebClient wc = new WebClient();
        wc.DownloadFile(downloadFrom, downloadTo);

        // Oczyszczanie pamięci
        wc.Dispose();
        downloadFrom = null;

        return File.ReadAllText(downloadTo);
    }

    /// <summary>
    /// Pobierz informacje dot. aktualizacji
    /// </summary>
    /// <param name="appName">Nazwa aplikacji</param>
    /// <returns>[0] = plik beta, [1] = plik release, [2] = plik minimum</returns>
    public static string[] GetUpdateData(string appName)
    {
        var downloadFrom = SERVER_URI + appName + @"/" + DATA_ID;
        var downloadTo = TEMP_PATH + appName + "_" + DATA_ID;

        // Czy plik istnieje?
        if (File.Exists(downloadTo))
        {
            // Usuń plik
            File.Delete(downloadTo);
        }

        // Utwórz obiekt WebClient i pobierz dane z serwera
        WebClient wc = new WebClient();
        wc.DownloadFile(downloadFrom, downloadTo);

        // Oczyszczanie pamięci
        wc.Dispose();
        downloadFrom = null;

        var returnArray = new string[3];

        // Przeanalizuj każdą linię pliku
        foreach (var line in File.ReadLines(downloadTo))
        {
            // Czy linia zawiera BETA_ID?
            if (line.Contains(BETA_ID))
            {
                // Załaduj wartość i przejdź do kolejnej linii
                returnArray[0] = line.Split('=')[1];
                continue;
            }
            // Czy linia zawiera RELEASE_ID?
            else if (line.Contains(RELEASE_ID))
            {
                // Załaduj wartość i przejdź do kolejnej linii
                returnArray[1] = line.Split('=')[1];
                continue;
            }
            // Czy linia zawiera MINIMUM_ID?
            else if (line.Contains(MINIMUM_ID))
            {
                // Załaduj wartość i przejdź do kolejnej linii
                returnArray[2] = line.Split('=')[1];
                continue;
            }
        }

        // Oczyszczanie pamięci
        File.Delete(downloadTo);
        downloadTo = null;

        return returnArray;
    }

    /// <summary>
    /// Sprawdź czy aktualizcja jest dostępna
    /// </summary>
    /// <param name="fileArray">[0] = plik beta, [1] = plik release, [2] = plik minimum</param>
    /// <param name="includeBeta">Sprawdzić wersję beta?</param>
    /// <returns>Czy znaleziono aktualizcaję?</returns>
    public static bool IsUpdateAvailible(string[] fileArray, string appName)
    {
        var appVersion = GetActiveVersion().Replace(".", "");
        var betaVersion = fileArray[0].Replace(".", "");
        var releaseVersion = fileArray[1].Replace(".", "");

        // Czy sprawdzić wersję beta?
        if (INCLUDE_BETA)
        {
            // Czy dostępna jest aktualizcaja beta?
            if (int.Parse(betaVersion) > int.Parse(appVersion))
            {
                return true;
            }
        }

        // Czy dostępna jest aktualizcaja?
        if (int.Parse(releaseVersion) > int.Parse(appVersion))
        {
            return true;
        }

        // Brak aktualizacji
        return false;
    }

    /// <summary>
    /// Sprawdź i zwróć werjśe zainstalowanej aplikacji
    /// </summary>
    /// <returns>Wersja aplikacji</returns>
    public static string GetActiveVersion()
    {
        // Sprawdź wersję pliku
        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(STARTUP_PATH);

        return fvi.FileVersion;
    }

    /// <summary>
    /// Sprawdź i zwróć wersję dostępnej aktualizacji
    /// </summary>
    /// <param name="fileArray">[0] = plik beta, [1] = plik release, [2] = plik minimum</param>
    /// <param name="includeBeta">Sprawdzić wersję beta?</param>
    /// <returns>Wersja aktualizacji</returns>
    public static string GetUpdateVersion(string[] fileArray, string appName)
    {
        var appVersion = GetActiveVersion().Replace(".", "");
        var betaVersion = fileArray[0].Replace(".", "");
        var releaseVersion = fileArray[1].Replace(".", "");

        // Czy sprawdzić wersję beta?
        if (INCLUDE_BETA)
        {
            // Czy dostępna jest aktualizcaja beta?
            if (int.Parse(betaVersion) > int.Parse(appVersion))
            {
                return fileArray[0] + " (BETA)";
            }
        }

        // Czy dostępna jest aktualizcaja?
        if (int.Parse(releaseVersion) > int.Parse(appVersion))
        {
            return fileArray[1];
        }

        // Brak aktualizacji
        return string.Empty;
    }

    /// <summary>
    /// Sprawdź czy aktualizcja typu force jest dostępna
    /// </summary>
    /// <param name="fileArray">[0] = plik beta, [1] = plik release, [2] = plik minimum</param>
    /// <returns>Czy należy wymusić aktualizację?</returns>
    public static bool IsForceUpdateAvailible(string[] fileArray)
    {
        var appVersion = GetActiveVersion().Replace(".", "");
        var minimumVersion = fileArray[2].Replace(".", "");

        if (int.Parse(minimumVersion) > int.Parse(appVersion))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Pobierz podaną aktualizcję
    /// </summary>
    /// <param name="fileArray">[0] = plik beta, [1] = plik release</param>
    /// <param name="appName">Nazwa aplikacji</param>
    /// <param name="includeBeta">Pobrać wersję beta?</param>
    public static void DownloadUpdate(ProgressBar pb, TextBlock tb, string[] fileArray, string appName)
    {
        var downloadFrom = string.Empty;
        var downloadTo = string.Empty;

        // Czy pobrać wersję beta?
        if (INCLUDE_BETA)
        {
            downloadFrom = SERVER_URI + appName + @"/" + BETA_ID + @"/" + fileArray[0] + ".exe";
            downloadTo = TEMP_PATH + fileArray[0];
        }
        else
        {
            downloadFrom = SERVER_URI + appName + @"/" + RELEASE_ID + @"/" + fileArray[1] + ".exe";
            downloadTo = TEMP_PATH + fileArray[1];
        }

        // Czy plik istnieje?
        if (File.Exists(downloadTo))
        {
            // Usuń plik
            File.Delete(downloadTo);
        }

        // Utwórz obiekt WebClient i pobierz dane z serwera
        WebClient wc = new WebClient();
        wc.DownloadProgressChanged += (sender, args) =>
        {
            // Zaktualizuj % ukończenia
            UPDATE_PROGRESS = args.ProgressPercentage;

            // Wywołaj w wątku UI
            Application.Current.Dispatcher.Invoke(delegate
            {
                pb.Value = UPDATE_PROGRESS;
                tb.Text = UPDATE_PROGRESS + "%";
            });
        };
        wc.DownloadFileCompleted += (sender, args) =>
        {
            // Zaktualizuj % ukończenia
            UPDATE_PROGRESS = 100;

            // Wywołaj w wątku UI
            Application.Current.Dispatcher.Invoke(delegate
            {
                pb.Value = UPDATE_PROGRESS;
                tb.Text = UPDATE_PROGRESS + "%";
            });

            // Oczyszczanie pamięci
            wc.Dispose();
            downloadFrom = null;
            downloadTo = null;
        };
        wc.DownloadFileAsync(new Uri(downloadFrom), downloadTo);

        // Oczekiwanie na pobranie pliku
        while (UPDATE_PROGRESS < 100)
        {
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Zainstaluj pobraną aktualizację
    /// </summary>
    /// <param name="fileArray">[0] = plik beta, [1] = plik release</param>
    /// <param name="includeBeta">Zainstalować wersję beta?</param>
    public static void InstallUpdate(string[] fileArray)
    {
        var updatePath = string.Empty;

        // Czy zainstalować wersję beta?
        if (INCLUDE_BETA)
        {
            updatePath = TEMP_PATH + fileArray[0];
        }
        else
        {
            updatePath = TEMP_PATH + fileArray[1];
        }

        // Usuń plik
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Arguments = "/C choice /C Y /N /D Y /T 2 & DEL " + "\"" + STARTUP_PATH + "\"";
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.FileName = "cmd.exe";
            Process.Start(psi);
        }

        // Zastąp plik
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Arguments = "/C choice /C Y /N /D Y /T 3 & MOVE " + "\"" + updatePath + "\"" + " \"" + STARTUP_PATH + "\"";
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.FileName = "cmd.exe";
            Process.Start(psi);
        }

        // Uruchom plik
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Arguments = "/C choice /C Y /N /D Y /T 4 & " + "\"" + STARTUP_PATH + "\"";
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.CreateNoWindow = true;
            psi.FileName = "cmd.exe";
            Process.Start(psi);
        }
    }
}
