using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MarkdownSharp;
using System.Text.RegularExpressions;

namespace EmaPersonalWiki
{
    class StatefulCheckboxPattern : IInlinePattern
    {
        public static Regex CheckBoxesRegex = new Regex(@"^\s*\[([\sxX])\](.*)$", RegexOptions.Multiline);
        private int mCheckboxIndex = 0; //which is the state

        public string Transform(string fragment)
        {
            return CheckBoxesRegex.Replace(fragment, transform);
        }

        private string transform(Match m)
        {
            var isChecked = !string.IsNullOrEmpty(m.Groups[1].Value.Trim());
            var label = m.Groups[2].Value;

   			string replacement = "<div class='ema-task'>";
			string js = @"onclick=""window.external.sendMessage('checkbox', '" + mCheckboxIndex + @"');""";
			if (isChecked) {
				replacement += "<label class='ema-task-finished'><input type='checkbox' checked='checked' " + js + "/>";
			}
			else {
				replacement += "<label><input type='checkbox' " + js + "/>";
			}
			replacement += label + "</label></div>";

            mCheckboxIndex++;

            return replacement;
        }
    }
}
