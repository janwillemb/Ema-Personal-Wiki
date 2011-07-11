using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmaPersonalWiki
{
    class HistoryItem
    {
        public HistoryItem() 
        { 
        }
        public HistoryItem(string pageName)
        {
            PageName = pageName;
            Title = pageName;
        }

        public static HistoryItem CreateVirtual(string title, string content)
        {
            return new HistoryItem
            {
                PageName = Guid.NewGuid().ToString(),
                IsVirtual = true,
                Content = content,
                Title = title
            };
        }

        public string PageName { get; set; }
        public string Content { get; set; }
        public int ScrollPosition { get; set; }

        public bool IsVirtual { get; set; }
        public string Title { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as HistoryItem;
            if (other == null || other.PageName == null || this.PageName == null)
            {
                return false;
            }

            return this.PageName.Equals(other.PageName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override int GetHashCode()
        {
            return this.PageName.GetHashCode();
        }

    }
}
