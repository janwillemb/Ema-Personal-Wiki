using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EmaPersonalWiki.Properties;

namespace EmaPersonalWiki
{
    static class FileHelpers
    {
        /// <summary>
        /// try to read / write a file 3 times. 
        /// </summary>
        public static void DoRetryableFileIO(Action a)
        {
            IOException ex = null;
            for (int i = 0; i < Settings.Default.NumberOfRetriesAfterFileFailure; i++)
            {
                ex = null;
                try
                {
                    a.Invoke();
                    break;
                }
                catch (IOException e)
                {
                    ex = e;
                    Thread.Sleep(500);
                }
            }

            if (ex != null)
            {
                throw new Exception("Error in file operation", ex);
            }
        }
    }
}
