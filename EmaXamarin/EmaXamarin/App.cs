using EmaXamarin.Api;
using EmaXamarin.Pages;
using Xamarin.Forms;

namespace EmaXamarin
{
    public class App : Application
    {
        private readonly EmaWikiPage _emaWikiPage;

        public App(IWikiStorage wikiStorage, IFileRepository fileRepository)
        {
            var service = new PageService(wikiStorage, new HtmlWrapper(fileRepository), new MarkdownImpl());
            _emaWikiPage = new EmaWikiPage(service);
            MainPage = new NavigationPage(_emaWikiPage);
        }

        protected override void OnStart()
        {
            _emaWikiPage.GoHome();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}