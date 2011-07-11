using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using System.Net;
using System.IO;
using EmaPersonalWiki.Properties;

namespace EmaPersonalWiki
{
    class UpgradeCheck
    {
        private bool mHasUpgrade;
        public bool HasUpgrade
        {
            get
            {
                return mHasUpgrade;
            }
        }
        private string mUpgradeText;
        public string UpgradeText
        {
            get
            {
                return mUpgradeText;
            }
        }
        private string mUpgradeCommand;
        public string UpgradeCommand
        {
            get
            {
                return mUpgradeCommand;
            }
        }
        private bool mShouldAskFirst;
        public bool ShouldAskFirst
        {
            get
            {
                return mShouldAskFirst;
            }
        }

        string mAvailableVersion;
        public string AvailableVersion
        {
            get
            {
                return mAvailableVersion;
            }
        }

        private const int CURRENT_VERSION = 11; 

        public void Start()
        {
            new Action(() => checkUpgrade()).BeginInvoke(null, null);
        }

        private static string baseUrl = "http://www.janwillemboer.nl/ema/windows/";
        private void checkUpgrade()
        {
            bool upgrade = false;

            try
            {
                Thread.Sleep(10000);

                var versionInfo = XDocument.Load(baseUrl + "versioninfo.xml");
                var latest = (from v in versionInfo.Descendants("upgrade")
                              where v.Attribute("version") != null && int.Parse(v.Attribute("version").Value) > CURRENT_VERSION
                              orderby int.Parse(v.Attribute("version").Value) descending
                              select v).FirstOrDefault();

                if (latest == null)
                    return;

                //there is an upgrade
                upgrade = true;

                mUpgradeText = latest.Element("info") != null ? latest.Element("info").Value : string.Empty;
                var number = latest.Attribute("version").Value;
                mAvailableVersion = number;
                mShouldAskFirst = latest.Attribute("ask") != null &&
                    latest.Attribute("ask").Value.Equals("true", StringComparison.CurrentCultureIgnoreCase);

                if (number == Settings.Default.SkipUpdate)
                {
                    upgrade = false;
                    return;
                }

                //create a dir to download to
                var tempPath = Path.Combine(Environment.ExpandEnvironmentVariables("%TEMP%"), "EmaPersonalWiki.v" + number);
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                using (var infoFile = File.CreateText(Path.Combine(tempPath, "ema.info")))
                {
                    infoFile.WriteLine("InstallDir=" + AppDomain.CurrentDomain.BaseDirectory);
                }

                //download the files
                var wc = new WebClient();
                foreach (var f in latest.Elements("file"))
                {
                    if (f.Attribute("name") == null)
                        continue;

                    var fileName = f.Attribute("name").Value;

                    wc.DownloadFile(baseUrl + "v" + number + "/" + fileName, Path.Combine(tempPath, fileName));
                }

                mUpgradeCommand = Path.Combine(tempPath, latest.Attribute("installer").Value);
            }

            catch (Exception)
            {
                //if there was an error, still point out that there is 
                //an update, which then can be downloaded manually.
                if (upgrade)
                {
                    mShouldAskFirst = true;
                    mUpgradeText = @"After you click 'Yes', a browser Window with the Ema Personal Wiki homepage will be opened.";
                    mUpgradeCommand = "http://www.janwillemboer.nl/blog/ema-personal-wiki";
                }
            }
            finally
            {
                mHasUpgrade = upgrade;
            }

        }


    }
}
