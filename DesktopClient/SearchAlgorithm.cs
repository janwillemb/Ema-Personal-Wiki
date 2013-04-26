using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EmaPersonalWiki
{
    public static class SearchAlgorithm
    {
        private static readonly Regex _splitExpr = new Regex(@"\W");

        public static SearchResult SearchPage(string pageName, string textToSearch, string query)
        {
            //beautiful search algorithm that is worthy of becoming the new Google
            var retval = new SearchResult
            {
                PageName = pageName,
                Relevance = 0.0,
                Snippet = string.Empty
            };

            string originalText = textToSearch;
            textToSearch = textToSearch.ToLower();
            string originalQuery = query;
            query = query.ToLower();
            var queryWords = getQueryWords(query).ToArray();

            if (!queryWords.Any())
            {
                return retval;
            }

            if (pageName.IndexOf(query, StringComparison.OrdinalIgnoreCase) > -1)
            {
                retval.Relevance = 1.0;
                createSnippet(retval, originalText, 0);
            }
            else
            {
                var queryPos = textToSearch.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                if (queryPos > -1)
                {
                    retval.Relevance = 0.9;

                    createSnippet(retval, originalText, queryPos);
                }
                else
                {
                    var relevancePart = 0.8 / queryWords.Count();
                    foreach (var word in queryWords)
                    {
                        if (pageName.IndexOf(word, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            retval.Relevance += relevancePart;
                            if (string.IsNullOrEmpty(retval.Snippet))
                            {
                                createSnippet(retval, originalText, 0);
                            }
                        }
                        else
                        {
                            var pos = textToSearch.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                            if (pos > -1)
                            {
                                retval.Relevance += relevancePart;

                                if (string.IsNullOrEmpty(retval.Snippet))
                                {
                                    createSnippet(retval, originalText, pos);
                                }
                            }
                        }
                    }
                }
            }

            //highlight words in snippet
            foreach (var word in getQueryWords(originalQuery))
            {
                var re = new Regex(word, RegexOptions.IgnoreCase);
                retval.Snippet = re.Replace(retval.Snippet, highlightWord);
            }
            return retval;
        }

        private static string highlightWord(Match word)
        {
            return "<strong>" + word.Value + "</strong>";
        }

        private static IEnumerable<string> getQueryWords(string query)
        {
            return
                from s in _splitExpr.Split(query)
                where !string.IsNullOrEmpty(s) && s.Trim().Length > 0
                select s;
        }

        private static void createSnippet(SearchResult retval, string text, int foundAtPos)
        {
            int snippetStart = Math.Max(0, foundAtPos - 50);
            int snippetStop = Math.Min(text.Length, snippetStart + 100);
            retval.Snippet = text.Substring(snippetStart, snippetStop - snippetStart);
        }
    }
}
