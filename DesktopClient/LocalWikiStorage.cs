using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmaPersonalWiki
{
    class LocalWikiStorage : WikiStorage
    {
        protected override string GetFileContentsInner(string normalizedPageName)
        {
            var file = getFileOfPage(normalizedPageName);
            var contents = string.Empty;
            if (file.Exists)
            {
                FileHelpers.DoRetryableFileIO(() =>
                {
                    using (var fr = file.OpenText())
                    {
                        contents = fr.ReadToEnd();
                    }
                });
            }

            return contents;
        }

        private FileInfo getFileOfPage(string pageName)
        {
            return new FileInfo(System.IO.Path.Combine(App.StorageDirectory, string.Concat(GetSafePageName(pageName), Extension)));
        }

        public override void SavePage(string pageName, string text)
        {
            var file = getFileOfPage(pageName);
            if (file.Exists)
            {
                FileHelpers.DoRetryableFileIO(file.Delete);
            }

            FileHelpers.DoRetryableFileIO(() =>
            {
                using (var sw = new StreamWriter(file.OpenWrite()))
                {
                    sw.Write(text);
                    sw.Close();
                }
            });
        }

        public override List<SearchResult> Find(string query)
        {
            var retval = new List<SearchResult>();
            foreach (var file in App.StorageDirectoryInfo.GetFiles("*" + Extension))
            {
                var file2 = file;
                FileHelpers.DoRetryableFileIO(() =>
                    {
                        using (var fr = file2.OpenText())
                        {
                            var contents = fr.ReadToEnd();

                            var result = SearchAlgorithm.SearchPage(file2.Name.Substring(0, file2.Name.Length - Extension.Length), contents, query);
                            if (result.Relevance > 0)
                            {
                                retval.Add(result);
                            }
                        }
                    });
            }
            return retval;
        }

        public override List<SearchResult> RecentChanges()
        {
            var retval = new List<SearchResult>();
            foreach (var file in App.StorageDirectoryInfo.GetFiles("*" + Extension).OrderByDescending(x => x.LastWriteTimeUtc))
            {
                var name = file.Name.Substring(0, file.Name.Length - Extension.Length);
                if (file.Length > 0)
                {
                    retval.Add(new SearchResult { PageName = name, Snippet = file.LastWriteTimeUtc.ToShortDateString() + " " + file.LastWriteTimeUtc.ToShortTimeString() });
                }
            }
            return retval;
        }

    }
}
