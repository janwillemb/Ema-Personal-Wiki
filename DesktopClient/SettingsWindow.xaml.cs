using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EmaPersonalWiki.Properties;

namespace EmaPersonalWiki
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            showStorageDir();
        }

        private void showStorageDir()
        {
            var fromSettings = Settings.Default.DropboxDir;
            textBoxStorageDir.Text = App.StorageDirectory;
            if (!string.IsNullOrEmpty(fromSettings))
            {
                labelAutomatically.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            var savePath = App.StorageDirectory;
            
            Settings.Default.DropboxDir = string.Empty;
            Settings.Default.Save();

            switch (MessageBox.Show("Should I try to use the Dropbox directory?", "Ema Personal Wiki asks", MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Yes:
                    App.GetStorageDir(true, false);
                    break;

                case MessageBoxResult.No:
                    App.GetStorageDir(false, false);
                    break;
            }

            if (string.IsNullOrEmpty(App.StorageDirectory))
            {
                //application can't run this way
                App.StorageDirectory = savePath;
            }
            showStorageDir();
        }

        private void buttonStyling_Click(object sender, RoutedEventArgs e)
        {
            var cssFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "style.css");
            try
            {
                var pi = new System.Diagnostics.ProcessStartInfo("notepad.exe", cssFile);
                System.Diagnostics.Process.Start(pi);
            }
            catch (Exception)
            {
                MessageBox.Show("Sorry, I could not open the css file. You can try to find or create it yourself in the application directory.", "Ema Personal Wiki apologizes");
            }

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
