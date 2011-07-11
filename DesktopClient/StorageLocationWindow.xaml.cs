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
using System.Windows.Forms;
using EmaPersonalWiki.Properties;

namespace EmaPersonalWiki
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class StorageLocationWindow : Window
    {
        public StorageLocationWindow()
        {
            InitializeComponent();
            textBoxDirectory.Text = Settings.Default.DropboxDir;
            if (string.IsNullOrEmpty(textBoxDirectory.Text))
            {
                textBoxDirectory.Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PersonalWiki");
            }
        }

        private void buttonSelect_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxDirectory.Text = dialog.SelectedPath;
            }
        }

        public string SelectedPath
        {
            get
            {
                return textBoxDirectory.Text;
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
