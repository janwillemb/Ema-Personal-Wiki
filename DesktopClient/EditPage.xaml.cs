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
using System.IO;
using System.Web;

namespace EmaPersonalWiki
{
    /// <summary>
    /// Interaction logic for EditPage.xaml
    /// </summary>
    public partial class EditPage : Window
    {
        private readonly PagesDal _dal;
        private readonly string _pageName;

        public EditPage(string pageName)
        {
            InitializeComponent();

            _dal = new PagesDal(new LocalWikiStorage(), new DesktopHtmlWrapper());
            _pageName = pageName;

            Title = Title + " " + _pageName;
            textBox1.Text = _dal.GetTextOfPage(pageName);
            textBox1.AllowDrop = true;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            _dal.SavePage(_pageName, textBox1.Text);
            Close();
        }

        private void textBox1_Drop(object sender, DragEventArgs e)
        {
            string fileName = null;
            try
            {
                var f = (string[])e.Data.GetData("FileNameW");
                fileName = f[0];
            }
            catch (Exception)
            { }

            if (fileName == null)
                return;

            if (MessageBox.Show("Copy this file to the wiki?", "Ema Personal Wiki asks", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                return;

            while (true)
            {
                try
                {
                    var fileToCopy = new FileInfo(fileName);

                    var newFileName = string.Concat(WikiStorage.GetSafePageName(_pageName), ".", HttpUtility.UrlEncode(fileToCopy.Name));
                    var newFile = new FileInfo(System.IO.Path.Combine(App.StorageDirectory, newFileName));
                    if (newFile.Exists)
                    {
                        if (MessageBox.Show("The file already exists. Should it be replaced with the new file?", "Ema Personal Wiki asks", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                        {
                            break;
                        }
                        newFile.Delete();
                    }

                    fileToCopy.CopyTo(newFile.FullName);

                    var isImage = new string[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(newFile.Extension.ToLower());

                    textBox1.SelectedText = string.Format("{2}[{0}](emafile:{1})", fileToCopy.Name, newFileName, isImage ? "!" : string.Empty);
                    break;
                }
                catch (Exception ex)
                {
                    if (MessageBox.Show("Sorry, the file could not be copied. The following excuse is provided:\n" + ex.Message + "\n\nTry again?",
                                    "Ema Personal Wiki apologizes", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
                    {
                        break;
                    }
                }
            }
        }

        private void textBox1_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }
    }
}
