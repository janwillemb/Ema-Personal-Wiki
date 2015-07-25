using System.Collections.Generic;

namespace EmaXamarin.Api
{
    public interface IWikiStorage
    {
        IEnumerable<SearchResult> Find(string query);
        string GetFileContents(string pageName);
        SearchResult[] RecentChanges();
        void SavePage(string pageName, string text);
    }
}