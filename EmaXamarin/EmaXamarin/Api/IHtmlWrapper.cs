using System.Collections.Generic;

namespace EmaXamarin.Api
{
    public interface IHtmlWrapper
    {
        string Wrap(string title, string contents);
        string ReplaceFileReferences(string html);
        IEnumerable<string> WrapLines(string title, IEnumerable<string> contents);
    }
}