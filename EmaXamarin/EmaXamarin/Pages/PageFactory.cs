using EmaXamarin.Api;

namespace EmaXamarin.Pages
{
    public class PageFactory
    {
        private PageService _pageService;
        private IFileRepository _fileRepository;
        private IExternalBrowserService _externalBrowserService;
        private ApplicationEvents _applicationEvents;

        private PageFactory()
        {
        }

        public static void Initialize(PageService pageService, IFileRepository fileRepository, IExternalBrowserService externalBrowserService, ApplicationEvents applicationEvents)
        {
            Current = new PageFactory
            {
                _pageService = pageService,
                _fileRepository = fileRepository,
                _externalBrowserService = externalBrowserService,
                _applicationEvents = applicationEvents
            };
        }

        public static PageFactory Current { get; private set; }

        public EmaWikiPage CreateEmaWikiPage()
        {
            return new EmaWikiPage(_pageService, _externalBrowserService);
        }

        public EditFilePage CreateEditFilePage(string pageName)
        {
            return new EditFilePage(pageName, _pageService);
        }

        public SettingsPage CreateSettingsPage()
        {
            return new SettingsPage(_fileRepository, _externalBrowserService, _applicationEvents);
        }
    }
}