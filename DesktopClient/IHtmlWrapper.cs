using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmaPersonalWiki
{
    public interface IHtmlWrapper
    {
        string Wrap(string title, string contents);
        string ReplaceFileReferences(string html);
    }
}
