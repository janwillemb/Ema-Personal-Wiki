using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EmaPersonalWiki
{
    public class SearchResult
    {

        public string PageName { get; set; }
        public double Relevance { get; set; }
        public string Snippet { get; set; }

    }
}
