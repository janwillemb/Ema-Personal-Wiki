using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

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