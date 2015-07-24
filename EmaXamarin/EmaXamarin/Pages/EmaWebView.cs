using System;
using System.Text.RegularExpressions;
using EmaXamarin.Api;
using Xamarin.Forms;

namespace EmaXamarin.Pages
{
    /// <summary>
    /// ema-specific functionality around the webview. For convenience sake this class inherits from WebView, 
    /// but it could also be a decorator.
    /// </summary>
    internal class EmaWebView : WebView
    {
        private readonly IExternalBrowserService _externalBrowserService;
        private static readonly Regex EmaUrlRegex = new Regex(@"ema:(.+)");
        private static readonly Regex CommandRegex = new Regex(@"emacmd:(.+)");

        public event EventHandler<RequestPageEventArgs> RequestPage;
        public event EventHandler RequestEdit;

        public EmaWebView(IExternalBrowserService externalBrowserService)
        {
            _externalBrowserService = externalBrowserService;
            VerticalOptions = LayoutOptions.FillAndExpand;
            Navigating += WebViewOnNavigating;
        }

        private void WebViewOnNavigating(object sender, WebNavigatingEventArgs args)
        {
            var m = EmaUrlRegex.Match(args.Url);
            if (m.Success)
            {
                args.Cancel = true;
                GoTo(m.Groups[1].Value);
            }
            else
            {
                m = CommandRegex.Match(args.Url);
                if (m.Success)
                {
                    args.Cancel = true;
                    switch (m.Groups[1].Value)
                    {
                        case "Edit":
                            Edit();
                            break;
                    }
                }
            }

            if (!args.Cancel)
            {
                args.Cancel = true;
                //open external links in external browser
                _externalBrowserService.OpenUrl(args.Url);
            }
        }

        private void GoTo(string pageName)
        {
            var args = new RequestPageEventArgs {PageName = pageName};
            OnRequestPage(args);
        }

        private void Edit()
        {
            OnRequestEdit(new EventArgs());
        }

        protected virtual void OnRequestPage(RequestPageEventArgs e)
        {
			if (RequestPage != null) 
            	RequestPage.Invoke(this, e);
        }

        protected virtual void OnRequestEdit(EventArgs e)
        {
			if (RequestEdit != null)
            	RequestEdit.Invoke(this, e);
        }
    }
}