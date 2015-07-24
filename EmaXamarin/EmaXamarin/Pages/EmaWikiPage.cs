using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    public class EmaWikiPage : ContentPage
    {
        private readonly PageService _pageService;
        private readonly IExternalBrowserService _externalBrowserService;
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
            _externalBrowserService = externalBrowserService;

            _searchBar = new SearchBar
            {
                Placeholder = "Search in wiki",
                IsVisible = false
            };
            _searchBar.SearchButtonPressed += SearchBarOnSearchButtonPressed;

            //prominent: the webview.
            _webView = new EmaWebView(externalBrowserService);
            _webView.RequestPage += (sender, args) => GoTo(args.PageName);
            _webView.RequestEdit += (sender, args) => EditCurrentPage();

            Content = new StackLayout
            {
                Children =
                {
                    _searchBar,
                    _webView
                }
            };

            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Home",
                Command = new Command(() => GoTo(PageService.DefaultPage)),
                Order = ToolbarItemOrder.Primary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Search",
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
                Text = "Edit",
                Command = new Command(EditCurrentPage),
                Order = ToolbarItemOrder.Primary
            });
            ToolbarItems.Add(new ToolbarItem
            {
                Text = "Settings",
                Command = new Command(Settings),
                Order = ToolbarItemOrder.Secondary
            });
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
                var currentPage = _pageHistory.Pop();
                GoTo(currentPage);
            }
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