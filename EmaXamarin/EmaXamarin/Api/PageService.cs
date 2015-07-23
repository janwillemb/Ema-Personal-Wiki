using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmaXamarin.Api
{
    /// <summary>
    /// retrieves, transforms and saves pages. 
    /// </summary>
    public class PageService
    {
        public const string DefaultPage = "Home";

        private readonly IWikiStorage _storage;
        private readonly IHtmlWrapper _wrapper;
        private readonly IMarkdown _markdown;

        public PageService(IWikiStorage storage, IHtmlWrapper wrapper, IMarkdown markdown)
        {
            _storage = storage;
            _wrapper = wrapper;
            _markdown = markdown;
        }

        public string GetHtmlOfPage(string pageName)
        {
            var html = _markdown.Transform(_storage.GetFileContents(pageName));
            html = _wrapper.ReplaceFileReferences(html);

            if (string.IsNullOrEmpty(html))
            {
                html = "<span style='color: Silver'>(This page is blank. Click the Edit button to add content.)</span>";
            }

            return _wrapper.Wrap(pageName, html);
        }

        public void SavePage(string pageName, string text)
        {
            _storage.SavePage(pageName, text);
        }

        public string RecentChanges()
        {
            var sb = new StringBuilder();
            foreach (var sr in _storage.RecentChanges())
            {
                sb.AppendFormat(@"
                        <div class='ema-searchresult'>
                            <div class='ema-searchresult-link'>
                                <a href='ema:{0}'>{0}</a> - {1} {2}
                            </div>
                        </div>",
                    sr.PageName, sr.Snippet);
            }

            return _wrapper.Wrap("Recent changes", sb.ToString());
        }

        public IEnumerable<string> Find(string query)
        {
            return _wrapper.WrapLines("Search results for \"" + query + "\"", FindInner(query));
        }

        private IEnumerable<string> FindInner(string query)
        {
            var results = _storage.Find(query);
            bool hadResult = false;

            foreach (var result in results.OrderByDescending(x => x.Relevance))
            {
                hadResult = true;
                yield return string.Format(@"
                        <div class='ema-searchresult'>
                            <div class='ema-searchresult-link'><a href='ema:{0}'>{0}</a></div>
                            <div class='ema-searchresult-snippet'>{1}</div>
                        </div>", result.PageName, result.Snippet);
            }

            if (!hadResult)
            {
                yield return "<div class='ema-search-noresults'>No results found</div>";
            }
        }

        public string GetTextOfPage(string pageName)
        {
            return _storage.GetFileContents(pageName);
        }
    }
}