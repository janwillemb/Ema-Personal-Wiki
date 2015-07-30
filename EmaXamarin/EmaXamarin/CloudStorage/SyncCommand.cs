namespace EmaXamarin.CloudStorage
{
    public class SyncCommand
    {
        public SyncType Type { get; set; }
        public SyncedFile File { get; set; }

        public override string ToString()
        {
            return Type + " " + File.Name;
        }

        public static SyncCommand Upload(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.Upload,
                File = file
            };
        }

        public static SyncCommand Download(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.Download,
                File = file
            };
        }

        public static SyncCommand DeleteLocal(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.DeleteLocal,
                File = file
            };
        }

        public static SyncCommand DeleteRemote(SyncedFile file)
        {
            return new SyncCommand
            {
                Type = SyncType.DeleteRemote,
                File = file
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