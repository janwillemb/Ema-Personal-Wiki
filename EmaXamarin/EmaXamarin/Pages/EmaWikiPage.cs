using System.Collections.Generic;
using System.Text.RegularExpressions;
using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    public class EmaWikiPage : ContentPage
    {
        private readonly PageService _pageService;
        private readonly EmaWebView _webView;
        private readonly Stack<string> _pageHistory = new Stack<string>();
        private bool _frist;

        /// <summary>
        /// constructor; builds the page and controls.
        /// </summary>
        /// <param name="pageService"></param>
        public EmaWikiPage(PageService pageService)
        {
            _pageService = pageService;

            //prominent: the webview.
            _webView = new EmaWebView();
            _webView.RequestPage += (sender, args) => GoTo(args.PageName);
            _webView.RequestEdit += (sender, args) => EditCurrentPage();

            Content = new StackLayout
            {
                Children =
                {
                    _webView
                }
            };

            var editItem = new ToolbarItem
            {
                Text = "Edit",
                Order = ToolbarItemOrder.Secondary,
                Command = new Command(EditCurrentPage)
            };
            ToolbarItems.Add(editItem);


            _frist = true;
        }

        public void GoHome()
        {
            GoTo(PageService.DefaultPage);
        }

        private void EditCurrentPage()
        {
            Navigation.PushAsync(new EditFilePage(_pageHistory.Peek(), _pageService));
        }

        public void GoTo(string page)
        {
            Title = "Wiki - " + page;
            _pageHistory.Push(page);
            var htmlSource = new HtmlWebViewSource { Html = _pageService.GetHtmlOfPage(page) };
            _webView.Source = htmlSource;
        }

        protected override void OnAppearing()
        {
            if (!_frist)
            {
                //refresh on re-entering this page.
                GoTo(_pageHistory.Peek());
            }
            _frist = false;
            base.OnAppearing();
        }

        protected override bool OnBackButtonPressed()
        {
            if (_pageHistory.Count > 1)
            {
                _pageHistory.Pop();
                GoTo(_pageHistory.Peek());
                return true;
            }
            else
            {
                return base.OnBackButtonPressed();
            }
        }
    }
}