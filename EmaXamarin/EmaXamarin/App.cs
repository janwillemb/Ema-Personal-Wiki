using System.IO;
using EmaXamarin.Api;
using EmaXamarin.CloudStorage;
using EmaXamarin.Pages;
using Xamarin.Forms;

namespace EmaXamarin
{
    public class App : Application
    {
        private readonly ApplicationEvents _applicationEvents;

        public App(IFileRepository fileRepository, IExternalBrowserService externalBrowserService)
        {
            var service = new PageService(new WikiStorage(fileRepository), new HtmlWrapper(fileRepository), new MarkdownImpl());
            _applicationEvents = new ApplicationEvents();

            try
            {
                fileRepository.StorageDirectory = PersistedState.CustomStorageDirectory;
            }
            catch 
            {
                //can't do much about it now.
            }

            PageFactory.Initialize(service, fileRepository, externalBrowserService, _applicationEvents);

            SyncBootstrapper.RefreshFromSyncInterval();
            SyncBootstrapper.RefreshForDropbox(fileRepository);

            MainPage = new NavigationPage(PageFactory.Current.CreateEmaWikiPage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            _applicationEvents.OnResumed();
        }
    }
}