using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MarkdownSharp;

namespace EmaPersonalWiki
{
    public class PagesDal
    {
        private readonly WikiStorage _storage;
        private readonly IHtmlWrapper _wrapper;
        private static readonly Regex _htmlTagsRegex = new Regex(@"\<a\s.+?\<\/a\>|\<[^\>]+\>");
        private const string _emaPlaceholder = "<_ema.ph_>";
        private int _checkboxIndex;
        private int _checkboxToTick;

        public PagesDal(WikiStorage storage, IHtmlWrapper wrapper)
        {
            _storage = storage;
            _wrapper = wrapper;
        }

        private string transform(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var parser = new Markdown();
            parser.ExtendWith(new StatefulCheckboxPattern());

            var t = parser.Transform(text);

            //prevent html from being replaced by wikiwords
            var htmlTags = new Queue<string>();
            t = _htmlTagsRegex.Replace(t, m =>
            {
                htmlTags.Enqueue(m.Groups[0].Value);
                return _emaPlaceholder;
            });

            //don't extend markdown with this pattern because it will destroy links
            t = new WikiWordsPattern().Transform(t);

            return _htmlTagsRegex.Replace(t, m =>
            {
                if (m.Groups[0].Value == _emaPlaceholder)
                {
                    return htmlTags.Dequeue();
                }
                else
                {
                    //new wikiword link
                    return m.Groups[0].Value;
                }
            });
        }

        public string GetHtmlOfPage(string pageName)
        {
            var html = transform(_storage.GetFileContents(pageName));
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

        public void SetCheckbox(string pageName, int checkbox)
        {
            var text = _storage.GetFileContents(pageName);

            //this isn't actually very threadsafe, but who cares for now
            _checkboxToTick = checkbox;
            _checkboxIndex = 0;
            text = StatefulCheckboxPattern.CheckBoxesRegex.Replace(text, ticker);
            SavePage(pageName, text);
        }

        private string ticker(Match m)
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
            if (results.Count > 0)
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
