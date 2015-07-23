using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmaXamarin.Api
{
    public static class SearchAlgorithm
    {
        private static readonly Regex SplitExpr = new Regex(@"\W");

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
            var queryWords = GetQueryWords(query).ToArray();

            if (!queryWords.Any())
            {
                return retval;
            }

            if (pageName.IndexOf(query, StringComparison.OrdinalIgnoreCase) > -1)
            {
                retval.Relevance = 1.0;
                CreateSnippet(retval, originalText, 0);
            }
            else
            {
                var queryPos = textToSearch.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                if (queryPos > -1)
                {
                    retval.Relevance = 0.9;

                    CreateSnippet(retval, originalText, queryPos);
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
                                CreateSnippet(retval, originalText, 0);
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
                                    CreateSnippet(retval, originalText, pos);
                                }
                            }
                        }
                    }
                }
            }

            //highlight words in snippet
            foreach (var word in GetQueryWords(originalQuery))
            {
                var re = new Regex(word, RegexOptions.IgnoreCase);
                retval.Snippet = re.Replace(retval.Snippet, HighlightWord);
            }
            return retval;
        }

        private static string HighlightWord(Match word)
        {
            return "<strong>" + word.Value + "</strong>";
        }

        private static IEnumerable<string> GetQueryWords(string query)
        {
            return
                from s in SplitExpr.Split(query)
                where !string.IsNullOrEmpty(s) && s.Trim().Length > 0
                select s;
        }

        private static void CreateSnippet(SearchResult retval, string text, int foundAtPos)
        {
            int snippetStart = Math.Max(0, foundAtPos - 50);
            int snippetStop = Math.Min(text.Length, snippetStart + 100);
            retval.Snippet = text.Substring(snippetStart, snippetStop - snippetStart);
        }
    }
}
