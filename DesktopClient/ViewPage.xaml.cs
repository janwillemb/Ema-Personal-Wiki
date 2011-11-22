using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Navigation;
using EmaPersonalWiki.Properties;
using System.Windows.Media;
using System.Diagnostics;
using System.Web;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Forms;

namespace EmaPersonalWiki
{
    public partial class ViewPage : Window
    {
        private FileSystemWatcher mWatcher;
        private HistoryItem mCurrentPage;
        private PagesDal mDal;
        private string mSearchWatermarkText;
        private Brush mSearchWatermarkBrush;
        private FontStyle mSearchWatermarkFontStyle;

        public ViewPage()
        {
            InitializeComponent();

            DataContext = this;

            if (!string.IsNullOrEmpty(App.Command))
            {
                mCurrentPage = new HistoryItem(App.Command);
            }
            else
            {
                mCurrentPage = new HistoryItem(PagesDal.DEFAULT_PAGE);
            }

            Keyboard.AddGotKeyboardFocusHandler(this, gotKeyboardFocus);
            Keyboard.AddLostKeyboardFocusHandler(this, lostKeyboardFocus);

            webBrowser1.Navigating += webBrowser1_Navigating;
            webBrowser1.LoadCompleted += webBrowser1_LoadCompleted;
            var jsinterop = new JavascriptInterop();
            webBrowser1.ObjectForScripting = jsinterop;
            jsinterop.InvokeFromJavascript += jsinterop_InvokeFromJavascript;
            webBrowser1.PreviewKeyDown += webBrowser1_KeyDown;

            mSearchWatermarkText = textBoxSearch.Text;
            mSearchWatermarkBrush = textBoxSearch.Foreground;
            mSearchWatermarkFontStyle = textBoxSearch.FontStyle;

            mDal = new PagesDal();

            EditCommand = new RelayCommand(() => buttonEdit_Click(null, null));
            InputBindings.Add(new KeyBinding(EditCommand, new KeyGesture(Key.E, ModifierKeys.Control)));
            BackCommand = new RelayCommand(() => buttonBack_Click(null, null));
            HomeCommand = new RelayCommand(() => buttonHome_Click(null, null));
            InputBindings.Add(new KeyBinding(HomeCommand, new KeyGesture(Key.Home, ModifierKeys.Alt)));
            FindCommand = new RelayCommand(() => textBoxSearch.Focus());
            InputBindings.Add(new KeyBinding(FindCommand, new KeyGesture(Key.OemQuestion, ModifierKeys.Control)));
            RecentCommand = new RelayCommand(() => recentModifications());
            InputBindings.Add(new KeyBinding(RecentCommand, new KeyGesture(Key.R, ModifierKeys.Control)));

            initWatcher();

            refresh();

            restorePosition();

            Loaded += (sender, e) => registerHotkey();
            Unloaded += (sender, e) => unregisterHotkey();
        }

        private HotKey _hk;
        private void registerHotkey()
        {
            try
            {
                unregisterHotkey();

                if (Settings.Default.UseHotKey)
                {
                    Keys hotkey;
                    if (Settings.Default.HotKey == 0)
                    {
                        hotkey = Keys.Home;
                    }
                    else
                    {
                        hotkey = (Keys)KeyInterop.VirtualKeyFromKey((Key)Settings.Default.HotKey);
                    }

                    _hk = new HotKey(ModifierKeys.Windows | ModifierKeys.Alt, hotkey, this);
                    _hk.HotKeyPressed += k =>
                    {
                        this.Activate();
                    };
                }
            }
            catch (Exception)
            {
            }
        }
        private void unregisterHotkey()
        {
            try
            {
                if (_hk != null)
                {
                    _hk.Dispose();
                    _hk = null;
                }
            }
            catch (Exception)
            {
            }
        }

        private void webBrowser1_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                buttonBack_Click(null, null);
            }
        }

        private bool mHasFocus = true; // a safe assumption to begin with
        private void lostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args)
        {
            mHasFocus = false;
        }
        private void gotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args)
        {
            mHasFocus = true;
        }

        private void restorePosition()
        {
            var s = Settings.Default;
            if (s.Width > 0)
            {
                Width = s.Width;
                Height = s.Height;
                Top = s.Top;
                Left = s.Left;

                if (s.Maximized)
                {
                    WindowState = System.Windows.WindowState.Maximized;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                Properties.Settings.Default.Top = RestoreBounds.Top;
                Properties.Settings.Default.Left = RestoreBounds.Left;
                Properties.Settings.Default.Height = RestoreBounds.Height;
                Properties.Settings.Default.Width = RestoreBounds.Width;
                Properties.Settings.Default.Maximized = true;
            }
            else
            {
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Maximized = false;
            }

            Properties.Settings.Default.Save();
        }

        private void initWatcher()
        {
            mWatcher = new FileSystemWatcher();
            mWatcher.Changed += mWatcher_Changed;
            mWatcher.Created += mWatcher_Changed;
            mWatcher.Deleted += mWatcher_Changed;
            mWatcher.Renamed += mWatcher_Changed;
            mWatcher.Error += mWatcher_Error;

            mWatcher.Path = App.StorageDirectory;
            mWatcher.EnableRaisingEvents = true;
        }

        private int mWatcherRetries = 0;
        void mWatcher_Error(object sender, ErrorEventArgs e)
        {
            mWatcherRetries++;
            mWatcher.EnableRaisingEvents = false;
            mWatcher.Changed -= mWatcher_Changed;
            mWatcher.Created -= mWatcher_Changed;
            mWatcher.Deleted -= mWatcher_Changed;
            mWatcher.Renamed -= mWatcher_Changed;
            mWatcher.Error -= mWatcher_Error;
            mWatcher.Dispose();
            mWatcher = null;

            if (mWatcherRetries >= Settings.Default.NumberOfRetriesAfterFileFailure)
            {
                throw new Exception("Error watching changes in Dropbox directory.");
            }
            Thread.Sleep(500);
            initWatcher();
        }

        void jsinterop_InvokeFromJavascript(object sender, InvokedEventArgs e)
        {
            if (e.Command.Equals("checkbox", StringComparison.InvariantCultureIgnoreCase))
            {
                int checkbox;
                if (int.TryParse(e.Parameters, out checkbox))
                {
                    mDal.SetCheckbox(mCurrentPage.PageName, checkbox);
                }
            }
        }

        private Stack<HistoryItem> mHistoryStack = new Stack<HistoryItem>();
        private static Regex internalUrlRegex = new Regex(@"^ema\:(.*)$");
        private static Regex filesUrlRegex = new Regex(@"^emafile\:(.*)$");
        void webBrowser1_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            var uri = e.Uri;
            if (uri == null || uri.OriginalString == "about:blank")
            {
                return;
            }

            e.Cancel = true;

            var m = internalUrlRegex.Match(uri.OriginalString);
            if (m.Success)
            {
                var pageName = HttpUtility.UrlDecode(m.Groups[1].Value);
                setCurrentPageWithHistory(pageName);

                var a = new Action(() =>
                {
                    Thread.Sleep(10);
                    refresh();
                }).BeginInvoke(null, null);
            }
            else
            {
                var file = uri.OriginalString;

                m = filesUrlRegex.Match(uri.OriginalString);
                if (m.Success)
                {
                    file = Path.Combine(App.StorageDirectory, m.Groups[1].Value);
                }

                Process.Start(file);
            }
        }

        private int getCurrentScrollPos()
        {
            try
            {
                int scrollPos = 0;
                Dispatcher.Invoke(new Action(() =>
                {
                    try
                    {
                        scrollPos = (int)webBrowser1.InvokeScript("getScrollPos");
                    }
                    catch
                    {
                    }
                }));

                return scrollPos;
            }
            catch (Exception)
            {
            }
            return 0;
        }

        private void setCurrentPageWithHistory(string newPage)
        {
            setCurrentPageWithHistory(new HistoryItem(newPage));
        }
        private void setCurrentPageWithHistory(HistoryItem newPage)
        {
            if (mHistoryStack.Count == 0 || !mHistoryStack.Peek().Equals(mCurrentPage))
            {
                //previous page on history stack
                mCurrentPage.ScrollPosition = getCurrentScrollPos();
                mHistoryStack.Push(mCurrentPage);
            }
            mCurrentPage = newPage;
        }

        void mWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                mCurrentPage.ScrollPosition = getCurrentScrollPos();
                refresh();
                mWatcherRetries = 0;
            }
            catch (Exception) { }
        }

        private void setTitle(string title)
        {
            Title = string.Format("{0} - Ema Personal Wiki", title);
        }

        private void refresh()
        {
            Dispatcher.Invoke(new Action(refreshInner));
        }

        private void refreshInner()
        {

            string html;

            if (mCurrentPage.IsVirtual)
            {
                html = mCurrentPage.Content;
            }
            else
            {
                html = mDal.GetHtmlOfPage(mCurrentPage.PageName);
            }
            setTitle(mCurrentPage.Title);

            webBrowser1.NavigateToString(html);

            //see webBrowser1_LoadCompleted
        }
        void webBrowser1_LoadCompleted(object sender, NavigationEventArgs e)
        {
            webBrowser1.InvokeScript("scrollTo", mCurrentPage.ScrollPosition);

            if (mHasFocus)
                focusOnBrowser();
        }

        private void focusOnBrowser()
        {
            try
            {
                webBrowser1.Focus();
                webBrowser1.InvokeScript("grabFocus"); //otherwise scrollwheel won't work unless you click the browser canvas
            }
            catch (Exception)
            {
                //this is not fatal
            }
        }

        public ICommand HomeCommand { get; set; }
        private void buttonHome_Click(object sender, RoutedEventArgs e)
        {
            setCurrentPageWithHistory(PagesDal.DEFAULT_PAGE);
            refresh();
        }

        public ICommand BackCommand { get; set; }
        private void buttonBack_Click(object sender, RoutedEventArgs e)
        {
            if (mHistoryStack.Count == 0)
            {
                return;
            }
            mCurrentPage = mHistoryStack.Pop();
            refresh();
        }

        public ICommand EditCommand { get; set; }
        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            var editWin = new EditPage(mCurrentPage.PageName);
            editWin.ShowDialog();
        }

        public ICommand RecentCommand { get; set; }
        private void recentModifications()
        {
            setCurrentPageWithHistory(HistoryItem.CreateVirtual("Recent changes", mDal.RecentChanges()));
            refresh();
        }

        private static readonly Regex _gotoPageRegex = new Regex(@"\>\s*(?<page>.+)");

        public ICommand FindCommand { get; set; }
        private void buttonFind_Click(object sender, RoutedEventArgs e)
        {
            var query = textBoxSearch.Text;

            if (!string.IsNullOrEmpty(query) && query != mSearchWatermarkText)
            {
                var m = _gotoPageRegex.Match(query);
                if (m.Success)
                {
                    //directly go to a page
                    string pageName = m.Groups["page"].Value;
                    setCurrentPageWithHistory(pageName);
                    refresh();
                    return;
                }

                //do a normal seacrh
                var findResultsHtml = mDal.Find(query);

                setCurrentPageWithHistory(HistoryItem.CreateVirtual("Search results", findResultsHtml));

                refresh();
            }
        }

        private void textBoxSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (textBoxSearch.Text.Equals(mSearchWatermarkText))
            {
                textBoxSearch.Foreground = Brushes.Black;
                textBoxSearch.Text = string.Empty;
                textBoxSearch.FontStyle = FontStyles.Normal;
            }
            else
            {
                textBoxSearch.SelectAll();
            }
        }

        private void textBoxSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxSearch.Text))
            {
                textBoxSearch.Text = mSearchWatermarkText;
                textBoxSearch.Foreground = mSearchWatermarkBrush;
                textBoxSearch.FontStyle = mSearchWatermarkFontStyle;
            }
        }

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            new About().ShowDialog();
        }

        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://www.janwillemboer.nl/blog/ema-personal-wiki"));
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();

            //hotkey may have been changed
            registerHotkey();

            //user may have changed the storage dir by now.
            if (!mWatcher.Path.Equals(App.StorageDirectory))
            {
                mWatcher.Path = App.StorageDirectory;
            }
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("You're about to delete page " + mCurrentPage.PageName +
                ". You can undo this if changes are synchronized with Dropbox.", "Ema Personal Wiki states", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                mDal.SavePage(mCurrentPage.PageName, string.Empty);
            }
        }

    }
}
