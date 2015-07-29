using System;
using System.Diagnostics;

namespace EmaXamarin
{
    public class Logging
    {
        public string Module { get; set; }

        public static Logging For<T>()
        {
            return new Logging {Module = typeof (T).Name};
        }

        public void Info(string msg)
        {
            Log("INFO", msg);
        }

        public void Error(string msg, Exception ex)
        {
            Log("ERROR", msg);
            if (ex != null)
            {
                Log("ERROR", ex.ToString());
            }
        }

        private void Log(string level, string msg)
        {
            Debug.WriteLine(level + ": " + Module + ": " + msg);
        }
    }
}