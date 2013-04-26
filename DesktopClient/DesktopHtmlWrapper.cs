using System;
using System.IO;

namespace EmaPersonalWiki
{
    class DesktopHtmlWrapper : IHtmlWrapper
    {
        private string _css;
        public DesktopHtmlWrapper()
        {
            var cssFile = new FileInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "style.css"));
            _css = string.Empty;
            if (cssFile.Exists)
            {
                FileHelpers.DoRetryableFileIO(() =>
                {
                    using (var fr = cssFile.OpenText())
                    {
                        _css = fr.ReadToEnd();
                    }
                });
            }
        }

        public string ReplaceFileReferences(string html)
        {
            return html.Replace(" src=\"emafile:", string.Concat(" src=\"file:///", App.StorageDirectory.Replace("\\", "/"), "/"));
        }

        public string Wrap(string title, string contents)
        {
            return @"
<html>
<head>
    <meta http-equiv='Content-Type' content='text/html;charset=UTF-8'/>
    <title>" + title + @" - Ema Personal Wiki</title>
    <style type='text/css'>" + _css + @"</style>
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
contents + @"  
</div></body>
</html>";


        }
    }
}
