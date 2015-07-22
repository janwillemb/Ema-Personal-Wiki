using System.Text.RegularExpressions;

namespace EmaXamarin.Api
{
    public class WikiWordsPattern
    {
        private static readonly Regex wikiWords = new Regex(@"
			(~)?(    #remember the previous character if it is the ignore marker and start a group for the actual match
            \p{Lu}       #start with uppercase letter
            \p{Ll}+      #one or more lowercase letters 
            \p{Lu}       #one uppercase letter 
            \w*          #and zero or more arbitrary characters in the same word
            |              #or
            \{           #start with a curly bracket
            [^\{\}]+       #anything inbetween that is not curly bracket
            \}            #end with curly br
            )        #close the group for the actual match
        ", RegexOptions.IgnorePatternWhitespace);

        public string Transform(string fragment)
        {
            return wikiWords.Replace(fragment, transform);
        }

        private string transform(Match m)
        {
            var replacement = m.Groups[2].Value;

            if (!string.IsNullOrEmpty(m.Groups[1].Value))
            {
                //ignore marker, return everything after the marker
                return replacement;
            }

            if (replacement.StartsWith("{") && replacement.EndsWith("}"))
            {
                //trim wikiword markers
                replacement = replacement.Substring(1, replacement.Length - 2);
            }

            return string.Format(@"<a href=""ema:{0}"">{0}</a>", replacement);
        }
    }
}