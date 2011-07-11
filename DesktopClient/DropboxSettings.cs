using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SQLite;

namespace EmaPersonalWiki
{
  class DropboxSettings
  {
    public static string GetDropboxPath()
    {
      //default location is 'my documents'
      var dropboxdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Dropbox");

      var sb = new SQLiteConnectionStringBuilder();
      var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"dropbox");

      var version = 0;
      var dropboxdb = Path.Combine(dir, "dropbox.db");
      if (!File.Exists(dropboxdb))
      {
        version = 1;
        dropboxdb = Path.Combine(dir, "config.db");
        if (!File.Exists(dropboxdb))
        {
          //default location
          return dropboxdir;
        }
      }


      sb.DataSource = dropboxdb;
      using (var conn = new SQLiteConnection(sb.ConnectionString))
      {
        conn.Open();
        if (version == 1)
        {
          using (var cmd = new SQLiteCommand(@"SELECT value FROM config WHERE key=""config_schema_version""", conn))
          {
            var result = cmd.ExecuteScalar();
            if (result is int)
            {
              version = (int)result;
            }
            else if (result is byte[])
            {
              var resultString = Encoding.UTF8.GetString((byte[])result);
              version = int.Parse(resultString);
            }
            else if (result is string)
            {
              version = int.Parse(result.ToString());
            }
            else
            {
              //unfortunately, default location
              return dropboxdir;
            }
          }
        }

        using (var cmd = new SQLiteCommand(@"select value from config where key=""dropbox_path""", conn))
        {
          var result = cmd.ExecuteScalar();
          if (result != null)
          {
            if (result is byte[])
            {
              dropboxdir = Encoding.UTF8.GetString((byte[])result);
            }
            else
            {
              dropboxdir = result.ToString();
            }

            if (version == 0)
            {
              var decoder = Encoding.UTF8.GetDecoder();
              var pwbytes = Convert.FromBase64String(dropboxdir);

              var decodedchars = new char[decoder.GetCharCount(pwbytes, 0, pwbytes.Length)];
              decoder.GetChars(pwbytes, 0, pwbytes.Length, decodedchars, 0);

              dropboxdir = new string(decodedchars).Substring(1).Replace(@"\u005C", @"\");
              dropboxdir = dropboxdir.Substring(0, dropboxdir.IndexOf("\n"));
            }
          }

          return dropboxdir;
        }
      }

    }

  }
}
