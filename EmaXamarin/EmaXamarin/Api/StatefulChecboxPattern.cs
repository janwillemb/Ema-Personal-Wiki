using System.Text.RegularExpressions;

namespace EmaXamarin.Api
{
    public class StatefulCheckboxPattern
    {
        public static Regex CheckBoxesRegex = new Regex(@"^\s*\[([\sxX])\](.*)$", RegexOptions.Multiline);
        private int _checkboxIndex; //which is the state

        public string Transform(string fragment)
        {
            return CheckBoxesRegex.Replace(fragment, TransformInner);
        }

        private string TransformInner(Match m)
        {
            var isChecked = !string.IsNullOrEmpty(m.Groups[1].Value.Trim());
            var label = m.Groups[2].Value;

            string replacement = "<div class='ema-task'>";
            string js = @"onclick=""window.location.href = '#checkbox" + _checkboxIndex + @"';""";
            if (isChecked)
            {
                replacement += "<label class='ema-task-finished'><input type='checkbox' checked='checked' " + js + "/>";
            }
            else
            {
                replacement += "<label><input type='checkbox' " + js + "/>";
            }
            replacement += label + "</label></div>";

            _checkboxIndex++;

            return replacement;
        }
    }
}