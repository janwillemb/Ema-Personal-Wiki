namespace EmaXamarin.CloudStorage
{
    public class SyncCommand
    {
        public string RemotePath { get; set; }
        public string LocalPath { get; set; }
        public SyncType Type { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Type + " " + Name;
        }

        public static SyncCommand Upload(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.Upload,
                Name = file.Name,
                LocalPath = file.LocalPath,
                RemotePath = file.RemotePath,
            };
        }

        public static SyncCommand Download(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.Download,
                Name = file.Name,
                LocalPath = file.LocalPath,
                RemotePath = file.RemotePath,
            };
        }

        public static SyncCommand DeleteLocal(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.DeleteLocal,
                Name = file.Name,
                LocalPath = file.LocalPath,
                RemotePath = file.RemotePath,
            };
        }

        public static SyncCommand DeleteRemote(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.DeleteRemote,
                Name = file.Name,
                LocalPath = file.LocalPath,
                RemotePath = file.RemotePath,
            };
        }
    }

    public enum SyncType
    {
        Upload,
        Download,
        DeleteRemote,
        DeleteLocal
    }
}