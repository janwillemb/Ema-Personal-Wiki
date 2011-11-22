using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using EmaPersonalWiki.Properties;
using MarkdownSharp;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace EmaPersonalWiki
{
    class PagesDal
    {
        public const string DEFAULT_PAGE = "Home";

        private const string EXTENSION = ".txt";
        private string mCss;

        public PagesDal()
        {
            var cssFile = new FileInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "style.css"));
            mCss = string.Empty;
            if (cssFile.Exists)
            {
                doRetryableFileIO(new Action(() =>
                {
                    using (var fr = cssFile.OpenText())
                    {
                        mCss = fr.ReadToEnd();
                    }
                }));
            }

        }

        /// <summary>
        /// try to read / write a file 3 times. 
        /// </summary>
        private void doRetryableFileIO(Action a)
        {
            IOException ex = null;
            for (int i = 0; i < Settings.Default.NumberOfRetriesAfterFileFailure; i++)
            {
                ex = null;
                try
                {
                    a.Invoke();
                    break;
                }
                catch (IOException e)
                {
                    ex = e;
                    Thread.Sleep(500);
                }
            }

            if (ex != null)
            {
                throw new Exception("Error in file operation", ex);
            }
        }

        public static Regex InvalidPageChars = new Regex(@"[^\w\-\.]");
        private FileInfo getFileOfPage(string pageName)
        {
            return new FileInfo(System.IO.Path.Combine(App.StorageDirectory, string.Concat(GetSafePageName(pageName), EXTENSION)));
        }

        public string GetSafePageName(string pageName)
        {
            return InvalidPageChars.Replace(pageName, "_");
        }


        public string GetTextOfPage(string pageName)
        {
            var file = getFileOfPage(pageName);
            var contents = string.Empty;
            if (file.Exists)
            {
                doRetryableFileIO(new Action(() =>
                {
                    using (var fr = file.OpenText())
                    {
                        contents = fr.ReadToEnd();
                    }
                }));
            }
            else if (pageName.Equals(DEFAULT_PAGE, StringComparison.InvariantCultureIgnoreCase))
            {
                //return default text
                return
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
            }

            return contents;
        }

        private static Regex htmlTagsRegex = new Regex(@"\<a\s.+?\<\/a\>|\<[^\>]+\>");
        private static string EMA_PLACEHOLDER = "<_ema.ph_>";
        private string transform(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var parser = new Markdown();
            parser.ExtendWith(new StatefulCheckboxPattern());
            
            var t = parser.Transform(text);

            //prevent html from being replaced by wikiwords
            var htmlTags = new Queue<string>();
            t = htmlTagsRegex.Replace(t, m =>
            {
                htmlTags.Enqueue(m.Groups[0].Value);
                return EMA_PLACEHOLDER;
            });

            //don't extend markdown with this pattern because it will destroy links
            t = new WikiWordsPattern().Transform(t);

            return htmlTagsRegex.Replace(t, m =>
            {
                if (m.Groups[0].Value == EMA_PLACEHOLDER)
                {
                    return htmlTags.Dequeue();
                }
                else
                {
                    //new wikiword link
                    return m.Groups[0].Value;
                }
            });
        }

        public string GetHtmlOfPage(string pageName)
        {
            var html = transform(GetTextOfPage(pageName));
            html = html.Replace(" src=\"emafile:", string.Concat(" src=\"file:///", App.StorageDirectory.Replace("\\", "/"), "/"));
            
            if (string.IsNullOrEmpty(html))
            {
                html = "<span style='color: Silver'>(This page is blank. Click the Edit button to add content.)</span>";
            }

            return getHtml(pageName, html);
        }

        public void SavePage(string pageName, string text)
        {
            var file = getFileOfPage(pageName);
            if (file.Exists)
            {
                doRetryableFileIO(new Action(() => file.Delete()));
            }

            doRetryableFileIO(new Action(() =>
            {
                using (var sw = new StreamWriter(file.OpenWrite()))
                {
                    sw.Write(text);
                    sw.Close();
                }
            }));
        }

        public void SetCheckbox(string pageName, int checkbox)
        {
            var text = GetTextOfPage(pageName);

            //this isn't actually very threadsafe, but who cares for now
            mCheckboxToTick = checkbox;
            mCheckboxIndex = 0;
            text = StatefulCheckboxPattern.CheckBoxesRegex.Replace(text, ticker);
            SavePage(pageName, text);
        }

        private int mCheckboxIndex;
        private int mCheckboxToTick;
        private string ticker(Match m)
        {
            string replacement;
            if (mCheckboxIndex == mCheckboxToTick)
            {
                replacement = m.Groups[0].Value
                    .Replace("[ ]", "[_]")
                    .Replace("[x]", "[ ]")
                    .Replace("[X]", "[ ]")
                    .Replace("[_]", "[x]");
            }
            else
            {
                replacement = m.Groups[0].Value;
            }
            mCheckboxIndex++;
            return replacement;
        }

        public string RecentChanges()
        {
            var sb = new StringBuilder();
            foreach (var file in App.StorageDirectoryInfo.GetFiles("*" + EXTENSION).OrderByDescending(x => x.LastWriteTimeUtc))
            {
                var name = file.Name.Substring(0, file.Name.Length - EXTENSION.Length);
                if (file.Length > 0)
                {
                    sb.AppendFormat(@"
                        <div class='ema-searchresult'>
                            <div class='ema-searchresult-link'>
                                <a href='ema:{0}'>{0}</a> - {1} {2}
                            </div>
                        </div>", 
                        name, file.LastWriteTimeUtc.ToShortDateString() , file.LastWriteTimeUtc.ToShortTimeString());
                }
            }

            return getHtml("Recent changes", sb.ToString());
        }

        public string Find(string query)
        {
            var results = new List<SearchResult>();
            foreach (var file in App.StorageDirectoryInfo.GetFiles("*" + EXTENSION))
            {
                doRetryableFileIO(new Action(() =>
                {
                    using (var fr = file.OpenText())
                    {
                        var contents = fr.ReadToEnd();

                        var result = new SearchResult(file.Name.Substring(0, file.Name.Length - EXTENSION.Length), contents, query);
                        if (result.Relevance > 0)
                        {
                            results.Add(result);
                        }
                    }
                }));
            }

            var sb = new StringBuilder();
            if (results.Count > 0)
            {
                foreach (var result in results.OrderByDescending(x => x.Relevance))
                {
                    sb.AppendFormat(@"
                    <div class='ema-searchresult'>
                        <div class='ema-searchresult-link'><a href='ema:{0}'>{0}</a></div>
                        <div class='ema-searchresult-snippet'>{1}</div>
                    </div>
                ", result.PageName, result.Snippet);
                }
            }
            else
            {
                sb.Append("<div class='ema-search-noresults'>No results found</div>");
            }

            return getHtml("Search results for \"" + query + "\"", sb.ToString());
        }

        private string getHtml(string title, string content)
        {
            return @"
<html>
<head>
    <meta http-equiv='Content-Type' content='text/html;charset=UTF-8'/>
    <title>" + title + @" - Ema Personal Wiki</title>
    <style type='text/css'>" + mCss + @"</style>
    <script type='text/javascript'>
        function getScrollPos() {
          try {
            return document.body.scrollTop;
          } catch (ex) {
            return 0;
          }
        }
        var scrollTryAgain = false;
        function scrollTo(pos) {
          try {
            document.body.scrollTop = pos;
            scrollTryAgain = false;
          } catch (ex) {
            //try again in a few moments
            if (scrollTryAgain == false) {
              scrollTryAgain = true;
              setTimeout(function() { scrollTo(pos); }, 100);
            } else {
              scrollTryAgain = false;
            }
          }
        }

        var grabFocusTryAgain = false;
        function grabFocus() {
          try {
            document.body.focus();
            grabFocusTryAgain = false;
          } catch (ex) {
            if (grabFocusTryAgain == false) {
              grabFocusTryAgain = true;
              setTimeout(grabFocus, 100);
            }
            else {
              grabFocusTryAgain = false;
            }
          }
        }
    </script>
</head>
<body oncontextmenu='return false'><div id='ema-body'>" +
content + @"  
</div></body>
</html>";
        }
    }
}
