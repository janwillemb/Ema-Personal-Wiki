using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmaXamarin.Api
{
    public abstract class WikiStorage : IWikiStorage
    {
        public static Regex InvalidPageChars = new Regex(@"[^\w\-\.]");
        private const string Extension = ".txt";

        public string GetFileContents(string pageName)
        {
            var isDefaultPage = pageName.Equals(PageService.DefaultPage, StringComparison.CurrentCultureIgnoreCase);

            pageName = GetSafePageName(pageName);
            var contents = GetFileContentsInner(pageName);

            if (string.IsNullOrEmpty(contents) && isDefaultPage)
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

        private static string GetSafePageName(string pageName)
        {
            return InvalidPageChars.Replace(pageName, "_") + Extension;
        }

        public void SavePage(string pageName, string text)
        {
            SavePageInner(GetSafePageName(pageName), text);
        }

        protected abstract void SavePageInner(string normalizedPageName, string text);
        public abstract SearchResult[] Find(string query);
        public abstract SearchResult[] RecentChanges();

    }
}
