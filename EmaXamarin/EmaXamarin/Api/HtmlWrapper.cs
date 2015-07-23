using System.Collections.Generic;
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

        public IEnumerable<string> WrapLines(string title, IEnumerable<string> contents)
        {
            yield return @"<html><head>";
            yield return @"    <meta http-equiv='Content-Type' content='text/html;charset=UTF-8'/>";
            yield return @"    <title>" + title + @" - Ema Personal Wiki</title>";
            yield return @"    <style type='text/css'>" + _css + @"</style></head>";
            yield return @" <body><div id='ema-body'>";
            foreach (var line in contents)
            {
                yield return line;
            };
            yield return @"</div></body></html>";
        }

        public string Wrap(string title, string contents)
        {
            return string.Join("\n", WrapLines(title, new[] {contents}));
        }
    }
}