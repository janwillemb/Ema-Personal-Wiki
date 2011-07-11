using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EmaPersonalWiki
{
    class SearchResult
    {

        private string mText;
        private string mOrgQuery;

        public SearchResult(string pageName, string textToSearch, string query)
        {
            //beautiful search algorithm that is worthy of becoming the new Google

            mText = textToSearch;
            mOrgQuery = query;
            this.PageName = pageName;

            Relevance = 0.0;
            Snippet = string.Empty;

            textToSearch = textToSearch.ToLower();
            query = query.ToLower();
            var queryWords = getQueryWords(query);

            if (queryWords.Count() == 0)
            {
                return;
            }

            var queryPos = textToSearch.IndexOf(query);
            if (queryPos > -1)
            {
                Relevance = 1.0;

                createSnippet(query, queryPos);
            }
            else
            {
                var relevancePart = 0.8 / queryWords.Count();
                foreach (var word in queryWords) 
                {
                    var pos = textToSearch.IndexOf(word);
                    if (pos > -1)
                    {
                        Relevance += relevancePart;

                        if (string.IsNullOrEmpty(Snippet))
                        {
                            createSnippet(word, pos);
                        }
                    }
                }
            }

            //highlight words in snippet
            foreach (var word in getQueryWords(mOrgQuery))
            {
                var re = new Regex(word, RegexOptions.IgnoreCase);
                Snippet = re.Replace(Snippet, highlightWord);
            }
        }

        private string highlightWord(Match word)
        {
            return "<strong>" + word.Value + "</strong>";
        }

        private Regex splitExpr = new Regex(@"\W");
        private IEnumerable<string> getQueryWords(string query)
        {
            return 
                from s in splitExpr.Split(query) 
                where !string.IsNullOrEmpty(s) && s.Trim().Length > 0 
                select s;
        }

        private void createSnippet(string highlight, int foundAtPos)
        {
            int snippetStart = Math.Max(0, foundAtPos - 50);
            int snippetStop = Math.Min(mText.Length, snippetStart + 100);
            Snippet = mText.Substring(snippetStart, snippetStop - snippetStart);
        }

        public string PageName { get; set; }
        public double Relevance { get; set; }
        public string Snippet { get; set; }

    }
}
