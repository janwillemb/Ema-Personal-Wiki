using System.IO;

namespace EmaXamarin.Api
{
    public class HtmlWrapper : IHtmlWrapper
    {
        private readonly IFileRepository _fileRepository;
        private readonly string _css;

        public HtmlWrapper(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
            _css = _fileRepository.GetText("style.css");
        }

        public string ReplaceFileReferences(string html)
        {
            return html.Replace(" src=\"emafile:", string.Concat(" src=\"file:///", _fileRepository.StorageDirectory.Replace("\\", "/"), "/"));
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
        
    </script>
</head>
<body oncontextmenu='return false'><div id='ema-buttons'><input type='button' value='Edit' onclick='location.href=""emacmd:Edit""'/><div id='ema-body'>" +
                   contents + @"  
</div></body>
</html>";
        }
    }
}