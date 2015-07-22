using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EmaXamarin.Api
{
    public class PageService
    {
        public const string DefaultPage = "Home";

        private readonly IWikiStorage _storage;
        private readonly IHtmlWrapper _wrapper;
        private readonly IMarkdown _markdown;
        private int _checkboxIndex;
        private int _checkboxToTick;

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

            if (String.IsNullOrEmpty(html))
            {
                html = "<span style='color: Silver'>(This page is blank. Click the Edit button to add content.)</span>";
            }

            return _wrapper.Wrap(pageName, html);
        }

        public void SavePage(string pageName, string text)
        {
            _storage.SavePage(pageName, text);
        }

        public void SetCheckbox(string pageName, int checkbox)
        {
            var text = _storage.GetFileContents(pageName);

            //this isn't actually very threadsafe, but who cares for now
            _checkboxToTick = checkbox;
            _checkboxIndex = 0;
            text = StatefulCheckboxPattern.CheckBoxesRegex.Replace(text, Ticker);
            SavePage(pageName, text);
        }

        private string Ticker(Match m)
        {
            string replacement;
            if (_checkboxIndex == _checkboxToTick)
            {
                replacement = m.Groups[0].Value
                    .Replace("[ ]", "[_]")
                    .Replace("[x]", "[ ]")
                    .Replace("[X]", "[ ]")
                    .Replace("[_]", "[x]");
            }
            else
            {
                replacement = m.Groups[0].Value;
            }
            _checkboxIndex++;
            return replacement;
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

        public string Find(string query)
        {
            var results = _storage.Find(query);

            var sb = new StringBuilder();
            if (results.Length > 0)
            {
                foreach (var result in results.OrderByDescending(x => x.Relevance))
                {
                    sb.AppendFormat(@"
                    <div class='ema-searchresult'>
                        <div class='ema-searchresult-link'><a href='ema:{0}'>{0}</a></div>
                        <div class='ema-searchresult-snippet'>{1}</div>
                    </div>
                ", result.PageName, result.Snippet);
                }
            }
            else
            {
                sb.Append("<div class='ema-search-noresults'>No results found</div>");
            }

            return _wrapper.Wrap("Search results for \"" + query + "\"", sb.ToString());
        }

        public string GetTextOfPage(string pageName)
        {
            return _storage.GetFileContents(pageName);
        }
    }
}