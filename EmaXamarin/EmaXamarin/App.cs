using System.IO;
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

            try
            {
                fileRepository.StorageDirectory = PersistedState.CustomStorageDirectory;
            }
            catch (IOException ex)
            {
                //can't do much about it now.
            }

            PageFactory.Initialize(service, fileRepository);

            _emaWikiPage = PageFactory.Current.CreateEmaWikiPage();
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