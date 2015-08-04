using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmaXamarin.Api;
using EmaXamarin.CloudStorage;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    public class EmaWikiPage : ContentPage
    {
        private readonly PageService _pageService;
        private readonly EmaWebView _webView;
        private readonly Stack<string> _pageHistory = new Stack<string>();
        private readonly SearchBar _searchBar;
        private const string SearchPageName = "ema:searchpage?query=";

        /// <summary>
        /// constructor; builds the page and controls.
        /// </summary>
        public EmaWikiPage(PageService pageService, IExternalBrowserService externalBrowserService)
        {
            _pageService = pageService;

            _searchBar = new SearchBar
            {
                Placeholder = "Search in wiki",
                IsVisible = false
            };
            _searchBar.SearchButtonPressed += SearchBarOnSearchButtonPressed;

            var syncProgress = new SyncProgressContentView {IsVisible = false};
            SyncBootstrapper.ShowSyncProgressIn(syncProgress);

            //prominent: the webview.
            _webView = new EmaWebView(externalBrowserService);
            _webView.RequestPage += (sender, args) => GoTo(args.PageName);
            _webView.RequestEdit += (sender, args) => EditCurrentPage();

            Content = new StackLayout
            {
                Children =
                {
                    _searchBar,
                    syncProgress,
                    _webView
                }
            };

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Home",
                Icon = "ic_menu_home.png",
                Command = new Command(() => GoTo(PageService.DefaultPage)),
                Order = ToolbarItemOrder.Primary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Edit",
                Icon = "ic_menu_edit.png",
                Command = new Command(EditCurrentPage),
                Order = ToolbarItemOrder.Primary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Search",
                Icon = "ic_menu_search.png",
                Order = ToolbarItemOrder.Secondary,
                Command = new Command(() =>
                {
                    _searchBar.Text = "";
                    _searchBar.IsVisible = !_searchBar.IsVisible;

                    if (_searchBar.IsVisible)
                    {
                        _searchBar.Focus();
                    }
                })
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Refresh",
                Command = new Command(Refresh),
                Order = ToolbarItemOrder.Secondary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Icon = "ic_menu_upload.png",
                Text = "Synchronize",
                Command = new Command(async () => await Synchronize()),
                Order = ToolbarItemOrder.Secondary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Preferences",
                Icon = "ic_menu_preferences.png",
                Command = new Command(Settings),
                Order = ToolbarItemOrder.Secondary
            });
        }

        private async Task Synchronize()
        {
            if (!SyncBootstrapper.CanSync)
            {
                await DisplayAlert("Synchronization", "Please configure the synchronizationoptions first (via settings)", "OK");
            }
            else
            {
                await SyncBootstrapper.StartNow();
                var ex = SyncBootstrapper.ConsumeSyncException();
                if (ex != null)
                {
                    await DisplayAlert("Not good", "An error occurred while synchronizing: " + ex.Message, "OK");
                }
            }
        }

        private void SearchBarOnSearchButtonPressed(object sender, EventArgs eventArgs)
        {
            string query = _searchBar.Text;

            GoTo(SearchPageName + query);
        }

        private void Search(string query)
        {
            var src = new HtmlWebViewSource {Html = ""};
            _webView.Source = src;

            Task.Run(() =>
            {
                foreach (var line in _pageService.Find(query))
                {
                    _webView.Eval("document.write('" + line.Replace("'", @"\'").Replace("\n", @"\n").Replace("\r", @"") + @"\n');");
                }
            });
        }

        private void EditCurrentPage()
        {
            var currentPage = _pageHistory.Peek();
            if (!currentPage.StartsWith(SearchPageName))
            {
                Navigation.PushAsync(PageFactory.Current.CreateEditFilePage(currentPage));
            }
        }

        private void Settings()
        {
            Navigation.PushAsync(PageFactory.Current.CreateSettingsPage());
        }

        public void GoTo(string page)
        {
            if (_pageHistory.Any())
            {
                var currentPage = _pageHistory.Peek();
                if (currentPage == page)
                {
                    //probably pressed the Home button while being on Home.
                    return;
                }
            }

            _pageHistory.Push(page);
            var isSearch = page.StartsWith(SearchPageName);

            if (isSearch)
            {
                var query = page.Substring(SearchPageName.Length);
                Title = "Search " + query;
                _searchBar.Text = query;
                Search(query);
            }
            else
            {
                Title = page;
                var htmlSource = new HtmlWebViewSource {Html = _pageService.GetHtmlOfPage(page)};
                _webView.Source = htmlSource;
            }

            _searchBar.IsVisible = isSearch;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!_pageHistory.Any())
            {
                //application startup

                //restart edit mode if the application was left in edit mode
                var pageInEditMode = PersistedState.PageInEditMode;
                if (!string.IsNullOrEmpty(pageInEditMode))
                {
                    GoTo(pageInEditMode);
                    EditCurrentPage();
                }
                else
                {
                    //start normally, go to the home page
                    GoTo(PageService.DefaultPage);
                }
            }
            else
            {
                //refresh on re-entering this page.
                Refresh();
            }
        }

        private void Refresh()
        {
            var currentPage = _pageHistory.Pop();
            GoTo(currentPage);
        }

        protected override bool OnBackButtonPressed()
        {
            if (_pageHistory.Count > 1)
            {
                _pageHistory.Pop();
                GoTo(_pageHistory.Pop());
                return true;
            }
            return base.OnBackButtonPressed();
        }
    }
}