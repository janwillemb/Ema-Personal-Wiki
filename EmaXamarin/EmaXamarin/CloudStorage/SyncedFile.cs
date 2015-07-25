using System;

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
        public SyncTimestamp CurrentSyncTimestamp { get; set; }
        public SyncTimestamp TimestampOnLastSync { get; set; }
        public SyncTimestamp TimestampAfterLastSync { get; set; }

        public bool NameEquals(string other)
        {
            return (other ?? string.Empty).Equals(Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}