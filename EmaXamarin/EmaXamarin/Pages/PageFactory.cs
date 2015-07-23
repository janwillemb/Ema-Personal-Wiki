using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmaXamarin.Api;

namespace EmaXamarin.Pages
{
    public class PageFactory
    {
        private PageService _pageService;
        private IFileRepository _fileRepository;
        private static PageFactory _instance;

        private PageFactory() { }

        public static void Initialize(PageService pageService, IFileRepository fileRepository)
        {
            _instance = new PageFactory
            {
                _pageService = pageService,
                _fileRepository = fileRepository
            };
        }

        public static PageFactory Current => _instance;

        public EmaWikiPage CreateEmaWikiPage()
        {
            return new EmaWikiPage(_pageService);
        }

        public EditFilePage CreateEditFilePage(string pageName)
        {
            return new EditFilePage(pageName, _pageService);
        }

        public SettingsPage CreateSettingsPage()
        {
            return new SettingsPage(_fileRepository);
        }
    }
}
