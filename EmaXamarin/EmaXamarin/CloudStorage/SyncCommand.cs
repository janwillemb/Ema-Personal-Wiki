namespace EmaXamarin.CloudStorage
{
    public class SyncCommand
    {
        public string RemotePath { get; set; }
        public string LocalPath { get; set; }
        public SyncType Type { get; set; }
        public string Name { get; set; }
    }

    public enum SyncType
    {
        Upload,
        Download
    }
}