using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using EmaPersonalWiki.Properties;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;

namespace EmaPersonalWiki
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string _storageDirectory = null;
        public static string Command;
        private static UpgradeCheck mUpgradeCheck;
        private static readonly Regex _commandRegex = new Regex(@"^(?:ema:(?://)?)?(.+)");

        public static string StorageDirectory
        {
            get
            {
                if (_storageDirectory == null)
                {
                    InitializeStorageDir(true, true);
                }
                return _storageDirectory;
            }
        }

        public static DirectoryInfo StorageDirectoryInfo
        {
            get
            {
                return new DirectoryInfo(StorageDirectory);
            }
        }


        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private static void closeSplash(SplashScreen splash)
        {
            if (splash == null)
                return;

            try
            {
                splash.Close(TimeSpan.FromSeconds(0.3));
            }
            catch (Exception)
            {
                try
                {
                    splash.Close(TimeSpan.Zero);
                }
                catch
                {
                    //whatever 
                }
            }

        }

        private static void log(string lines)
        {
            try
            {
                string log = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "ema.log");
                File.AppendAllText(log, DateTime.Now + ": " + lines + "\n\n");
            }
            catch (Exception)
            {
                //that's unfortunate, but not fatal
            }

        }

        [STAThread, DebuggerNonUserCode]
        public static void Main(string[] args)
        {
            SplashScreen splash = null;
            try
            {
                //we have to manage the splash screen ourselves, due to a bug in the splashscreen
                //Win32Exception when activating main form on Win XP.
                splash = new SplashScreen("splash.png");
                splash.Show(false);
            }
            catch (Exception)
            {
                //whatever
            }

            App app = new App();
            app.InitializeComponent();

            mUpgradeCheck = new UpgradeCheck();
            mUpgradeCheck.Start();

            closeSplash(splash);

            if (args.Length > 0)
            {
                var m = _commandRegex.Match(args[0]);
                if (m.Success)
                {
                    Command = m.Groups[1].Value;
                }
            }

            app.Run();
        }


        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            log(e.ToString());
            
            if (MessageBox.Show("Unfortunately, an error occurred from which Ema Personal Wiki could not recover.\n\nThe application will close. For what it's worth, the error message is:\n" + e.Exception.Message +
            "\n\nIf it annoys you, please report the error to ema@janwillemboer.nl.\nDo you want to copy the error details to the clipboard?",
            "Ema regrets to inform you", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Clipboard.SetText("To: ema@janwillemboer.nl\nSubject: Ema Personal Wiki error report\n\n" + e.Exception.ToString());
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (mUpgradeCheck != null)
            {
                if (mUpgradeCheck.HasUpgrade)
                {
                    if (!mUpgradeCheck.ShouldAskFirst || MessageBox.Show("There is a new version of Ema Personal Wiki. Would you like to install it now?\n\n"
                        + mUpgradeCheck.UpgradeText, "Ema Personal Wiki says", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        Process.Start(mUpgradeCheck.UpgradeCommand);
                    }
                    else
                    {
                        if (MessageBox.Show("Do you want to be remembered about this update the next time?", "Ema Personal Wiki asks", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            Settings.Default.SkipUpdate = mUpgradeCheck.AvailableVersion;
                            Settings.Default.Save();
                        }
                    }
                }
            }
        }
        
        internal static void InitializeStorageDir(bool useDropboxPath, bool fromAppStart)
        {
            //try from settings
            _storageDirectory = Settings.Default.DropboxDir;

            if (useDropboxPath)
            {
                if (string.IsNullOrEmpty(_storageDirectory) || !Directory.Exists(_storageDirectory))
                {
                    _storageDirectory = DropboxSettings.GetDropboxPath();
                    if (Directory.Exists(_storageDirectory))
                    {
                        _storageDirectory = Path.Combine(_storageDirectory, "PersonalWiki");
                        if (!Directory.Exists(_storageDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(_storageDirectory);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            var shouldSave = false;
            while (string.IsNullOrEmpty(_storageDirectory) || !Directory.Exists(_storageDirectory))
            {
                var slw = new StorageLocationWindow();
                if (slw.ShowDialog().GetValueOrDefault())
                {
                    shouldSave = true;
                    _storageDirectory = slw.SelectedPath;
                    if (!Directory.Exists(_storageDirectory))
                    {
                        try
                        {
                            Directory.CreateDirectory(_storageDirectory);
                        }
                        catch (Exception ex)
                        {
                            shouldSave = false;
                            MessageBox.Show("The directory could not be created because of the following reason.\n\n" + ex.Message);
                        }
                    }
                }
                else
                {
                    if (fromAppStart)
                    {
                        MessageBox.Show("Without a directory to store the files, the application can't run and will exit.");
                        App.Current.Shutdown();
                    }
                    return;
                }
            }

            if (shouldSave)
            {
                try
                {
                    Settings.Default.DropboxDir = _storageDirectory;
                    Settings.Default.Save();
                }
                catch (Exception)
                {
                }
            }
        }


        internal static void AssignStorageDirectory(string savePath)
        {
            _storageDirectory = savePath;
        }
    }
}
