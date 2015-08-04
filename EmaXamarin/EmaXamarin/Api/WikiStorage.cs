using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace EmaXamarin.Api
{
    public class WikiStorage : IWikiStorage
    {
        public static Regex InvalidPageChars = new Regex(@"[^\w\-\.]");
        protected const string Extension = ".txt";

        private readonly IFileRepository _fileRepository;

        public WikiStorage(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public string GetFileContents(string pageName)
        {
            var isDefaultPage = pageName.Equals(PageService.DefaultPage, StringComparison.CurrentCultureIgnoreCase);

            pageName = GetSafePageName(pageName);
            var contents = _fileRepository.GetText(pageName);

            if (string.IsNullOrEmpty(contents) && isDefaultPage)
            {
                //return default text
                using (var s = typeof (App).GetTypeInfo().Assembly.GetManifestResourceStream("EmaXamarin.DefaultHomeText.txt"))
                {
                    using (var reader = new StreamReader(s))
                    {
                        contents = reader.ReadToEnd();
                    }
                }
            }

            return contents ?? string.Empty;
        }

        private static string GetSafePageName(string pageName)
        {
            return InvalidPageChars.Replace(pageName, "_") + Extension;
        }

        public void SavePage(string pageName, string text)
        {
            _fileRepository.SaveText(GetSafePageName(pageName), text);
        }

        public void DeletePage(string pageName)
        {
            _fileRepository.DeleteFile(GetSafePageName(pageName));
        }

        public SearchResult[] RecentChanges()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SearchResult> Find(string query)
        {
            var results = new List<SearchResult>();
            foreach (string file in _fileRepository.EnumerateFiles("*" + Extension))
            {
                var contents = _fileRepository.GetText(file);
                var result = SearchAlgorithm.SearchPage(Path.GetFileNameWithoutExtension(file), contents, query);
                if (result.Relevance > 0)
                {
                    yield return result;
                }
            }
        }
    }
}