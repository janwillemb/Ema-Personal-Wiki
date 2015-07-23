using System;
using System.Collections.Generic;
using System.IO;
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

        public override IEnumerable<SearchResult> Find(string query)
        {
            var storageDir = new DirectoryInfo(_fileRepository.StorageDirectory);
            var results = new List<SearchResult>();
            foreach (var file in storageDir.GetFiles("*" + Extension))
            {
                var contents = _fileRepository.GetText(file.FullName);
                var result = SearchAlgorithm.SearchPage(Path.GetFileNameWithoutExtension(file.FullName), contents, query);
                if (result.Relevance > 0)
                {
                    yield return result;
                }
            }
        }
    }
}