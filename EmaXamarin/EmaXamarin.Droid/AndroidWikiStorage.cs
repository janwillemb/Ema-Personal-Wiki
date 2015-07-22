using System;
using EmaXamarin.Api;

namespace EmaXamarin.Droid
{
    internal class AndroidWikiStorage : WikiStorage
    {
        private readonly IFileRepository _fileRepository;

        public AndroidWikiStorage(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        protected override string GetFileContentsInner(string normalizedPageName)
        {
            return _fileRepository.GetText(normalizedPageName);
        }

        public override SearchResult[] RecentChanges()
        {
            throw new NotImplementedException();
        }

        protected override void SavePageInner(string normalizedPageName, string text)
        {
            _fileRepository.SaveText(normalizedPageName, text);
        }

        public override SearchResult[] Find(string query)
        {
            throw new NotImplementedException();
        }
    }
}