using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmaXamarin.Api
{
    public class MarkdownImpl : IMarkdown
    {
        private static readonly Regex HtmlTagsRegex = new Regex(@"\<a\s.+?\<\/a\>|\<[^\>]+\>");
        private const string EmaPlaceholder = "<_ema.ph_>";

        public string Transform(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var result = CommonMark.CommonMarkConverter.Convert(text);

            //prevent html from being replaced by wikiwords
            var htmlTags = new Queue<string>();
            result = HtmlTagsRegex.Replace(result, m =>
            {
                htmlTags.Enqueue(m.Groups[0].Value);
                return EmaPlaceholder;
            });

            //don't extend markdown with this pattern because it will destroy links
            result = new WikiWordsPattern().Transform(result);

            result = HtmlTagsRegex.Replace(result, m =>
            {
                if (m.Groups[0].Value == EmaPlaceholder)
                {
                    return htmlTags.Dequeue();
                }
                //new wikiword link
                return m.Groups[0].Value;
            });

            return result;
        }
    }
}