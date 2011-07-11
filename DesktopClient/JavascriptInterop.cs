using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace EmaPersonalWiki
{
    [ComVisible(true)]
    public class JavascriptInterop
    {
        internal event EventHandler<InvokedEventArgs> InvokeFromJavascript;

        [ComVisible(true)]
        public void sendMessage(String command, String parameters)
        {
            if (InvokeFromJavascript != null)
            {
                new Action(() =>
                {
                    InvokeFromJavascript.Invoke(this, new InvokedEventArgs { Command = command, Parameters = parameters });
                }).BeginInvoke(null, null);
            }
        }
    }

    internal class InvokedEventArgs : EventArgs
    {
        public string Command { get; set; }
        public string Parameters { get; set; }
    }
}
