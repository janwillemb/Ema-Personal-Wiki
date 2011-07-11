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
        public static string StorageDirectory;
        public static DirectoryInfo StorageDirectoryInfo
        {
            get
            {
                return new DirectoryInfo(StorageDirectory);
            }
        }

        public static string Command;

        private UpgradeCheck mUpgradeCheck;

        public App()
        {
            log("handle exceptions");

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            log("get storage dir");

            GetStorageDir(true, true);

            log("upgradecheck");

            mUpgradeCheck = new UpgradeCheck();
            mUpgradeCheck.Start();

            log("continue...");
        }

        private static readonly Regex _commandRegex = new Regex(@"^(?:ema:(?://)?)?(.+)");

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
            string log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ema.log");
            File.AppendAllText(log, DateTime.Now + ": " + lines + "\n\n");
        }

        [STAThread, DebuggerNonUserCode]
        public static void Main(string[] args)
        {
            log("show splashscreen");
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

            log("init app");
            App app = new App();
            app.InitializeComponent();

            log("close splash");
            closeSplash(splash);

            log("parse commandline");
            if (args.Length > 0)
            {
                var m = _commandRegex.Match(args[0]);
                if (m.Success)
                {
                    Command = m.Groups[1].Value;
                }
            }

            log("start app");
            app.Run();
        }


        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            log(e.ToString());
            
            if (MessageBox.Show("Unfortunately, an error occurred from which Ema Personal Wiki could not recover.\n\nThe application will close. For what it's worth, the error message is:\n" + e.Exception.Message +
            "\n\nIf it annoys you, please report the error to ema@janwillemboer.nl.\nDo you want to copy the error details to the clipboard?",
            "Ema regrets to inform you", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Clipboard.SetText("To: ema@janwillemboer.nl\nSubject: Ema Personal Wiki annoyance\n\n" + e.Exception.ToString());
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


        internal static void GetStorageDir(bool useDropboxPath, bool fromAppStart)
        {
            //try from settings
            StorageDirectory = Settings.Default.DropboxDir;

            if (useDropboxPath)
            {
                if (string.IsNullOrEmpty(StorageDirectory) || !Directory.Exists(StorageDirectory))
                {
                    StorageDirectory = DropboxSettings.GetDropboxPath();
                    if (Directory.Exists(StorageDirectory))
                    {
                        StorageDirectory = Path.Combine(StorageDirectory, "PersonalWiki");
                        if (!Directory.Exists(StorageDirectory))
                        {
                            try
                            {
                                Directory.CreateDirectory(StorageDirectory);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            var shouldSave = false;
            while (string.IsNullOrEmpty(StorageDirectory) || !Directory.Exists(StorageDirectory))
            {
                var slw = new StorageLocationWindow();
                if (slw.ShowDialog().GetValueOrDefault())
                {
                    shouldSave = true;
                    StorageDirectory = slw.SelectedPath;
                    if (!Directory.Exists(StorageDirectory))
                    {
                        try
                        {
                            Directory.CreateDirectory(StorageDirectory);
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
                Settings.Default.DropboxDir = StorageDirectory;
                Settings.Default.Save();
            }
        }

    }
}
