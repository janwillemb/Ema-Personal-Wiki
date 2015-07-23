using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmaXamarin.Api
{
    public abstract class WikiStorage : IWikiStorage
    {
        public static Regex InvalidPageChars = new Regex(@"[^\w\-\.]");
        protected const string Extension = ".txt";

        public string GetFileContents(string pageName)
        {
            var isDefaultPage = pageName.Equals(PageService.DefaultPage, StringComparison.CurrentCultureIgnoreCase);

            pageName = GetSafePageName(pageName);
            var contents = GetFileContentsInner(pageName);

            if (string.IsNullOrEmpty(contents) && isDefaultPage)
            {
                //return default text
                using (var s = typeof(App).GetTypeInfo().Assembly.GetManifestResourceStream("EmaXamarin.DefaultHomeText.txt"))
                {
                    using (var reader = new StreamReader(s))
                    {
                        contents = reader.ReadToEnd();
                    }
                }
            }

            return contents ?? string.Empty;
        }

        protected abstract string GetFileContentsInner(string normalizedPageName);

        private static string GetSafePageName(string pageName)
        {
            return InvalidPageChars.Replace(pageName, "_") + Extension;
        }

        public void SavePage(string pageName, string text)
        {
            SavePageInner(GetSafePageName(pageName), text);
        }

        protected abstract void SavePageInner(string normalizedPageName, string text);
        public abstract IEnumerable<SearchResult> Find(string query);
        public abstract SearchResult[] RecentChanges();

    }
}
