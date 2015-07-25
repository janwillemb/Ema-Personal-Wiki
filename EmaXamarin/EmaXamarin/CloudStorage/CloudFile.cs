using System;

namespace EmaXamarin.CloudStorage
{
    public class CloudFile
    {
        public string Name { get; set; }
        public DateTime? RemoteModifiedDateTime { get; set; }
        public DateTime? LocalModifiedDateTime { get; set; }
        public DateTime? LastSyncDateTime { get; set; }
        public long RemoteSize { get; set; }
        public long LocalSize { get; set; }
    }
}