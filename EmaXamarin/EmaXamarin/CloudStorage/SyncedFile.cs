using System;
using System.IO;

namespace EmaXamarin.CloudStorage
{
    public class SyncedFile
    {
        public SyncedFile()
        {
            CurrentSyncTimestamp = new SyncTimestamp();
            TimestampOnLastSync = new SyncTimestamp();
            TimestampAfterLastSync = new SyncTimestamp();
        }

        public string Name { get; set; }
        public string RemotePath { get; set; }

        public string LocalPath
        {
            get
            {
                if (LocalDirectory == null)
                {
                    return null;
                }
                return Path.Combine(LocalDirectory, Name);
            }
        }

        public string LocalDirectory { get; set; }
        public bool LocallyAbsent { get; set; }
        public bool LocallyDeleted { get; set; }
        public bool RemoteDeleted { get; set; }
        public SyncTimestamp CurrentSyncTimestamp { get; set; }
        public SyncTimestamp TimestampOnLastSync { get; set; }
        public SyncTimestamp TimestampAfterLastSync { get; set; }

        public bool NameEquals(string other)
        {
            return (other ?? string.Empty).Equals(Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}