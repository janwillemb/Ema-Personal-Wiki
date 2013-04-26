using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EmaPersonalWiki
{
    public abstract class WikiStorage
    {
        public static Regex InvalidPageChars = new Regex(@"[^\w\-\.]");
        public const string Extension = ".txt";
        public const string DefaultPage = "Home";

        public string GetFileContents(string pageName)
        {
            pageName = GetSafePageName(pageName);
            var contents = GetFileContentsInner(pageName);

            if (string.IsNullOrEmpty(contents) && pageName.Equals(DefaultPage, StringComparison.InvariantCultureIgnoreCase))
            {
                //return default text
                #region default page text
                contents =
                    @"Home
======

This is the homepage for your personal wiki. You can edit this page by clicking the Edit button in the toolbar.


Create a link to a new or existing page by surrounding a word with curly brackets like {Todo}, or by using a WikiWord (a word with mixed casing).


To synchronize the wiki pages between your desktop computer and an Android device, install Dropbox on the desktop computer and provide your
dropbox credentials in the Android app.


This personal wiki uses [Markdown formatting](http://en.wikipedia.org/wiki/Markdown#Syntax_examples) for text editing. 

![Ema Personal Wiki](http://janwillemboer.nl/ema/android/about_ema.png)  

Ema Personal Wiki, version 1.0, is a notebook with linkable pages for tracking your ideas, tasks, projects, notes, brainstorms - in short, your life - in the most flexible way.
If you have any questions, don't hesitate to contact the developer at ema@janwillemboer.nl or go to 
<http://www.janwillemboer.nl/blog/ema-personal-wiki>";
                #endregion
            }

            return contents ?? string.Empty;
        }

        protected abstract string GetFileContentsInner(string normalizedPageName);

        public static string GetSafePageName(string pageName)
        {
            return InvalidPageChars.Replace(pageName, "_");
        }

        public abstract void SavePage(string pageName, string text);
        public abstract List<SearchResult> Find(string query);
        public abstract List<SearchResult> RecentChanges(); 
        
    }
}
